# Proxy favicons — `favicons.yg-devworks.com`

L'app Cocktails affiche l'icône d'un package à partir du **favicon de son site**
(`homepage`). Plutôt que d'appeler un service tiers (Google) directement — ce qui
exposerait à ce tiers les domaines consultés — l'app passe par **ton proxy** sur
`yg-devworks.com`. Le proxy est le seul à parler au service en amont ; l'app ne parle
qu'à `yg-devworks.com`.

Côté app : `src/Cocktails/Controls/AppIcon.cs` (constante `FaviconProxy`).

## Contrat HTTP (ce que l'app attend)

```
GET https://favicons.yg-devworks.com/{domain}?sz=64
```

- **`{domain}`** : nom d'hôte nu, en minuscules (ex. `git-scm.com`, `code.visualstudio.com`).
  L'app l'extrait de `homepage` (`Uri.Host`) et l'URL-encode.
- **`sz`** (optionnel) : taille demandée en px (l'app envoie `64`). Défaut conseillé : 64.

### Réponses

| Cas | Statut | Corps |
|-----|--------|-------|
| Icône trouvée | **200** | `Content-Type: image/png`, PNG **carré ~`sz`px** |
| Domaine sans favicon / échec amont / timeout | **404** | (peu importe) |
| Domaine mal formé (hors `[a-z0-9.-]`) | **400** | (peu importe) |
| Méthode ≠ GET/HEAD | **405** | — |

Points importants :
- **Ne jamais renvoyer d'icône générique** (globe/placeholder) : l'app préfère son propre
  repli (badge avec l'initiale `F`/`C`). Un placeholder en 200 masquerait ce repli.
- L'app traite **tout non-2xx, non-image ou PNG indécodable comme « pas d'icône »** → badge.
  Donc en cas de doute côté serveur : renvoie **404**, pas un 200 dégradé.
- Format : **PNG** (Skia/Avalonia ne décode pas `.ico`). Si la source amont fournit de l'ICO,
  le proxy doit convertir en PNG.

### Cache

Les favicons changent rarement :
- `Cache-Control: public, max-age=2592000` (30 j) sur les 200.
- Cache disque/CDN par clé `{domain}/{sz}`, TTL long.
- **Negative cache** des 404 sur un TTL court (ex. 1 j) pour ne pas marteler l'amont.

## Source amont recommandée

Le endpoint gstatic de Google renvoie **directement un PNG** (pas de redirection) et un
**404** quand il n'a pas d'icône (ce que le proxy propage tel quel) :

```
https://t2.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=https://{domain}&size={sz}
```

(Alternative : DuckDuckGo `https://icons.duckduckgo.com/ip3/{domain}.ico` — mais renvoie de
l'ICO, à convertir en PNG. Ou, à terme, ton propre fetch + parsing du `<link rel="icon">`.)

## Implémentation de référence — nginx (reverse proxy + cache)

```nginx
proxy_cache_path /var/cache/nginx/favicons levels=1:2 keys_zone=favicons:10m
                 max_size=500m inactive=30d use_temp_path=off;

# Taille : chiffres uniquement, défaut 64.
map $arg_sz $fav_size {
    default            64;
    "~^[0-9]{1,3}$"    $arg_sz;
}

server {
    listen 443 ssl;
    http2 on;
    server_name favicons.yg-devworks.com;

    ssl_certificate     /etc/letsencrypt/live/favicons.yg-devworks.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/favicons.yg-devworks.com/privkey.pem;

    resolver 1.1.1.1 8.8.8.8 valid=300s ipv6=off;

    # Uniquement un nom d'hôte nu dans le chemin.
    location ~ "^/(?<domain>[a-z0-9.-]+)$" {
        set $up "https://t2.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=https://$domain&size=$fav_size";

        proxy_pass $up;
        proxy_ssl_server_name on;
        proxy_set_header Host t2.gstatic.com;
        proxy_set_header User-Agent "cocktails-favicon-proxy";

        proxy_connect_timeout 5s;
        proxy_read_timeout    5s;

        proxy_cache        favicons;
        proxy_cache_key    "$domain/$fav_size";
        proxy_cache_valid  200 30d;
        proxy_cache_valid  404 1d;         # negative cache
        add_header Cache-Control "public, max-age=2592000";
        add_header X-Cache-Status $upstream_cache_status;
    }

    location / { return 404; }   # chemins non conformes → 404
}
```

Notes :
- `proxy_pass` vers une variable **exige** un `resolver` (présent ci-dessus).
- gstatic renvoie **200 + PNG** si trouvé, **404** sinon : nginx propage le statut → l'app
  affiche l'icône ou son badge de repli. Rien d'autre à gérer.
- Certificat : `certbot --nginx -d favicons.yg-devworks.com` (le sous-domaine doit pointer
  vers le serveur en DNS).

## Alternative — Caddy (TLS auto, plus concis)

```caddy
favicons.yg-devworks.com {
    @dom path_regexp d ^/([a-z0-9.-]+)$
    handle @dom {
        rewrite * /faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=https://{re.d.1}&size=64
        reverse_proxy https://t2.gstatic.com {
            header_up Host t2.gstatic.com
        }
    }
    respond 404
}
```

(Cache : via le module `cache-handler` de Caddy, ou un CDN devant.)

## Vérification

```bash
# 200 + PNG carré
curl -sD- -o /tmp/i.png "https://favicons.yg-devworks.com/git-scm.com?sz=64" | head -n1
file /tmp/i.png            # -> PNG image data, 64 x 64

# 404 attendu (domaine sans favicon) -> l'app affiche le badge
curl -s -o /dev/null -w "%{http_code}\n" "https://favicons.yg-devworks.com/alembic.io?sz=64"
```
