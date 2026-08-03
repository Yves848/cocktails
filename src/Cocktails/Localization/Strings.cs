using System.Collections.Generic;

namespace Cocktails.Localization;

/// <summary>
/// Catalogue des traductions. Chaque clé mappe un tableau ordonné
/// [English, French, Spanish, German]. Les termes Homebrew (formula, cask, tap, brew…)
/// restent tels quels. Clé absente → renvoyée telle quelle (repérable) ; traduction vide
/// → repli sur l'anglais.
/// </summary>
public static class Strings
{
    public static string Get(string key, AppLanguage lang)
    {
        if (Map.TryGetValue(key, out var arr))
        {
            var v = arr[Index(lang)];
            return string.IsNullOrEmpty(v) ? arr[0] : v;
        }

        return key;
    }

    private static int Index(AppLanguage lang) => lang switch
    {
        AppLanguage.French => 1,
        AppLanguage.Spanish => 2,
        AppLanguage.German => 3,
        _ => 0,
    };

    // [EN, FR, ES, DE]
    private static readonly Dictionary<string, string[]> Map = new()
    {
        // Général / fenêtre
        ["App.Tagline"] = ["Your Homebrew package bar", "Votre bar à paquets Homebrew", "Tu bar de paquetes Homebrew", "Deine Homebrew-Paketbar"],
        ["Win.Minimize"] = ["Minimize", "Réduire", "Minimizar", "Minimieren"],
        ["Win.Maximize"] = ["Maximize / restore", "Agrandir / restaurer", "Maximizar / restaurar", "Maximieren / Wiederherstellen"],
        ["Win.Close"] = ["Close", "Fermer", "Cerrar", "Schließen"],

        // Navigation + titres d'écran
        ["Nav.Installed"] = ["Installed", "Installés", "Instalados", "Installiert"],
        ["Nav.Search"] = ["Search", "Rechercher", "Buscar", "Suchen"],
        ["Nav.Updates"] = ["Updates", "Mises à jour", "Actualizaciones", "Updates"],
        ["Nav.Maintenance"] = ["Maintenance", "Maintenance", "Mantenimiento", "Wartung"],
        ["Nav.Services"] = ["Services", "Services", "Servicios", "Dienste"],
        ["Nav.Taps"] = ["Taps", "Taps", "Taps", "Taps"],
        ["Nav.Settings"] = ["Settings", "Réglages", "Ajustes", "Einstellungen"],
        ["Nav.Help"] = ["Help", "Aide", "Ayuda", "Hilfe"],

        // Boutons communs
        ["Btn.Refresh"] = ["Refresh", "Rafraîchir", "Actualizar", "Aktualisieren"],
        ["Btn.Cancel"] = ["Cancel", "Annuler", "Cancelar", "Abbrechen"],
        ["Btn.Install"] = ["Install", "Installer", "Instalar", "Installieren"],
        ["Btn.Uninstall"] = ["Uninstall", "Désinstaller", "Desinstalar", "Deinstallieren"],
        ["Btn.Reinstall"] = ["Reinstall", "Réinstaller", "Reinstalar", "Neu installieren"],
        ["Btn.Update"] = ["Update", "Mettre à jour", "Actualizar", "Aktualisieren"],
        ["Btn.Pin"] = ["Pin", "Épingler", "Fijar", "Anheften"],
        ["Btn.Unpin"] = ["Unpin", "Désépingler", "Desfijar", "Lösen"],
        ["Btn.Add"] = ["Add", "Ajouter", "Añadir", "Hinzufügen"],
        ["Btn.Remove"] = ["Remove", "Retirer", "Quitar", "Entfernen"],
        ["Btn.Trust"] = ["Trust", "Confiance", "Confiar", "Vertrauen"],
        ["Btn.OpenPage"] = ["Open page ↗", "Ouvrir la page ↗", "Abrir página ↗", "Seite öffnen ↗"],
        ["Btn.Check"] = ["Check", "Vérifier", "Comprobar", "Prüfen"],

        // Filtres / segments
        ["Filter.All"] = ["All", "Tout", "Todo", "Alle"],
        ["Filter.Roots"] = ["Roots", "Racines", "Raíces", "Wurzeln"],
        ["Placeholder.Filter"] = ["Filter (name)…", "Filtrer (nom)…", "Filtrar (nombre)…", "Filtern (Name)…"],
        ["Placeholder.Search"] = ["Search Homebrew…", "Rechercher dans Homebrew…", "Buscar en Homebrew…", "Homebrew durchsuchen…"],
        ["Tooltip.Leaves"] = ["brew leaves — explicitly installed (hides auto dependencies)", "brew leaves — installés explicitement (masque les dépendances auto)", "brew leaves — instalados explícitamente (oculta dependencias automáticas)", "brew leaves — explizit installiert (blendet Auto-Abhängigkeiten aus)"],

        // Détail (volet partagé)
        ["Detail.SelectPackage"] = ["Select a package to see its details.", "Sélectionnez un paquet pour voir son détail.", "Selecciona un paquete para ver su detalle.", "Wähle ein Paket, um Details zu sehen."],
        ["Detail.SelectResult"] = ["Select a result to see its details.", "Sélectionnez un résultat pour voir son détail.", "Selecciona un resultado para ver su detalle.", "Wähle ein Ergebnis, um Details zu sehen."],
        ["Detail.Dependencies"] = ["DEPENDENCIES", "DÉPENDANCES", "DEPENDENCIAS", "ABHÄNGIGKEITEN"],
        ["Detail.DependencyTree"] = ["DEPENDENCY TREE", "ARBRE DES DÉPENDANCES", "ÁRBOL DE DEPENDENCIAS", "ABHÄNGIGKEITSBAUM"],
        ["Detail.Dependents"] = ["INSTALLED DEPENDENTS", "DÉPENDANTS INSTALLÉS", "DEPENDIENTES INSTALADOS", "INSTALLIERTE ABHÄNGIGE"],
        ["Detail.DependentsWarn"] = ["These packages would stop working if you uninstall it.", "Ces paquets cesseraient de fonctionner si vous le désinstallez.", "Estos paquetes dejarían de funcionar si lo desinstalas.", "Diese Pakete würden nicht mehr funktionieren, wenn du es deinstallierst."],
        ["Detail.InstalledVersion"] = ["Installed version", "Version installée", "Versión instalada", "Installierte Version"],
        ["Detail.LatestVersion"] = ["Latest version", "Dernière version", "Última versión", "Neueste Version"],
        ["Chip.Pinned"] = ["pinned", "épinglé", "fijado", "angeheftet"],
        ["Chip.Installed"] = ["installed", "installé", "instalado", "installiert"],
        ["Chip.AlreadyInstalled"] = ["already installed", "déjà installé", "ya instalado", "bereits installiert"],

        // Opérations par lot
        ["Tooltip.CheckUninstall"] = ["Select for batch uninstall", "Sélectionner pour une désinstallation par lot", "Seleccionar para desinstalación por lotes", "Für Stapel-Deinstallation auswählen"],
        ["Tooltip.CheckUpdate"] = ["Select for batch update", "Sélectionner pour une mise à jour par lot", "Seleccionar para actualización por lotes", "Für Stapel-Aktualisierung auswählen"],
        ["Batch.Selected"] = ["{0} package(s) selected", "{0} paquet(s) sélectionné(s)", "{0} paquete(s) seleccionado(s)", "{0} Paket(e) ausgewählt"],
        ["Batch.UncheckAll"] = ["Uncheck all", "Tout décocher", "Desmarcar todo", "Alle abwählen"],
        ["Batch.UninstallSelection"] = ["Uninstall selection", "Désinstaller la sélection", "Desinstalar selección", "Auswahl deinstallieren"],
        ["Batch.ReinstallSelection"] = ["Reinstall selection", "Réinstaller la sélection", "Reinstalar selección", "Auswahl neu installieren"],
        ["Batch.UpdateSelection"] = ["Update selection", "Mettre à jour la sélection", "Actualizar selección", "Auswahl aktualisieren"],

        // Mises à jour
        ["Btn.UpdateIndex"] = ["Update index", "Actualiser l'index", "Actualizar índice", "Index aktualisieren"],
        ["Tooltip.UpdateIndex"] = ["brew update — refreshes the formulae/casks database", "brew update — rafraîchit la base des formules/casks", "brew update — actualiza la base de fórmulas/casks", "brew update — aktualisiert die Formeln-/Casks-Datenbank"],
        ["Btn.UpdateAll"] = ["Update all", "Tout mettre à jour", "Actualizar todo", "Alle aktualisieren"],

        // Maintenance
        ["Maint.Subtitle"] = ["Cleanup & diagnostics", "Nettoyage & diagnostic", "Limpieza y diagnóstico", "Bereinigung & Diagnose"],
        ["Maint.CacheTitle"] = ["Cache cleanup", "Nettoyage du cache", "Limpieza de caché", "Cache-Bereinigung"],
        ["Maint.CacheDesc"] = ["Removes cached downloads and old versions (brew cleanup).", "Supprime les téléchargements en cache et les anciennes versions (brew cleanup).", "Elimina las descargas en caché y las versiones antiguas (brew cleanup).", "Entfernt zwischengespeicherte Downloads und alte Versionen (brew cleanup)."],
        ["Btn.Clean"] = ["Clean", "Nettoyer", "Limpiar", "Bereinigen"],
        ["Maint.OrphansTitle"] = ["Orphaned dependencies", "Dépendances orphelines", "Dependencias huérfanas", "Verwaiste Abhängigkeiten"],
        ["Maint.OrphansDesc"] = ["Removes formulae installed automatically and no longer needed (brew autoremove).", "Retire les formulae installées automatiquement et devenues inutiles (brew autoremove).", "Elimina las fórmulas instaladas automáticamente que ya no se necesitan (brew autoremove).", "Entfernt automatisch installierte, nicht mehr benötigte Formeln (brew autoremove)."],
        ["Maint.DiagTitle"] = ["Diagnostics", "Diagnostic", "Diagnóstico", "Diagnose"],
        ["Maint.DiagDesc"] = ["Checks the integrity of the Homebrew installation (brew doctor).", "Vérifie l'intégrité de l'installation Homebrew (brew doctor).", "Comprueba la integridad de la instalación de Homebrew (brew doctor).", "Prüft die Integrität der Homebrew-Installation (brew doctor)."],
        ["Btn.RunDoctor"] = ["Run brew doctor", "Lancer brew doctor", "Ejecutar brew doctor", "brew doctor ausführen"],
        ["Maint.MissingTitle"] = ["Missing dependencies", "Dépendances manquantes", "Dependencias faltantes", "Fehlende Abhängigkeiten"],
        ["Maint.MissingDesc"] = ["Finds installed formulae whose dependency is no longer installed (brew missing).", "Repère les formules installées dont une dépendance n'est plus installée (brew missing).", "Detecta fórmulas instaladas cuya dependencia ya no está instalada (brew missing).", "Findet installierte Formeln, deren Abhängigkeit nicht mehr installiert ist (brew missing)."],
        ["Maint.NoMissing"] = ["✓ No missing dependency", "✓ Aucune dépendance manquante", "✓ Ninguna dependencia faltante", "✓ Keine fehlende Abhängigkeit"],
        ["Maint.BackupSection"] = ["BACKUP (BREWFILE)", "SAUVEGARDE (BREWFILE)", "COPIA DE SEGURIDAD (BREWFILE)", "SICHERUNG (BREWFILE)"],
        ["Maint.ExportTitle"] = ["Export configuration", "Exporter la configuration", "Exportar configuración", "Konfiguration exportieren"],
        ["Maint.ExportDesc"] = ["Writes a Brewfile (taps, formulae, casks) — a versionable snapshot, handy to migrate machine (brew bundle dump).", "Écrit un Brewfile (taps, formulae, casks) — instantané versionnable, utile pour migrer de machine (brew bundle dump).", "Escribe un Brewfile (taps, fórmulas, casks) — instantánea versionable, útil para migrar de equipo (brew bundle dump).", "Schreibt ein Brewfile (Taps, Formeln, Casks) — versionierbarer Snapshot, praktisch für den Rechnerwechsel (brew bundle dump)."],
        ["Btn.Export"] = ["Export…", "Exporter…", "Exportar…", "Exportieren…"],
        ["Maint.ImportTitle"] = ["Import a configuration", "Importer une configuration", "Importar una configuración", "Konfiguration importieren"],
        ["Maint.ImportDesc"] = ["Installs everything missing from a Brewfile (brew bundle install).", "Installe tout ce qui manque d'après un Brewfile (brew bundle install).", "Instala todo lo que falta según un Brewfile (brew bundle install).", "Installiert alles Fehlende aus einem Brewfile (brew bundle install)."],
        ["Btn.Import"] = ["Import…", "Importer…", "Importar…", "Importieren…"],

        // Services
        ["Svc.started"] = ["active", "actif", "activo", "aktiv"],
        ["Svc.scheduled"] = ["scheduled", "planifié", "programado", "geplant"],
        ["Svc.stopped"] = ["stopped", "arrêté", "detenido", "gestoppt"],
        ["Svc.none"] = ["inactive", "inactif", "inactivo", "inaktiv"],
        ["Svc.error"] = ["error", "erreur", "error", "Fehler"],
        ["Btn.Start"] = ["Start", "Démarrer", "Iniciar", "Starten"],
        ["Btn.Stop"] = ["Stop", "Arrêter", "Detener", "Stoppen"],
        ["Btn.Restart"] = ["Restart", "Redémarrer", "Reiniciar", "Neu starten"],

        // Taps
        ["Taps.Subtitle"] = ["formulae / casks repositories", "dépôts de formules / casks", "repositorios de fórmulas / casks", "Formeln-/Casks-Repositorys"],
        ["Placeholder.Tap"] = ["user/repo (e.g. felixkratz/formulae)", "utilisateur/dépôt (ex. felixkratz/formulae)", "usuario/repo (ej. felixkratz/formulae)", "Benutzer/Repo (z. B. felixkratz/formulae)"],
        ["Tap.Official"] = ["official", "officiel", "oficial", "offiziell"],
        ["Tap.ThirdParty"] = ["third-party", "tiers", "terceros", "Drittanbieter"],
        ["Tap.Trusted"] = ["✓ trusted", "✓ de confiance", "✓ de confianza", "✓ vertraut"],
        ["Tap.ToApprove"] = ["to approve", "à approuver", "por aprobar", "zu genehmigen"],
        ["Tooltip.TapTrusted"] = ["Homebrew is allowed to load this tap", "Homebrew est autorisé à charger ce tap", "Homebrew puede cargar este tap", "Homebrew darf diesen Tap laden"],
        ["Tooltip.TapToApprove"] = ["Untrusted tap — click “Trust” to allow it", "Tap non approuvé — cliquez « Confiance » pour l'autoriser", "Tap no aprobado — pulsa «Confiar» para permitirlo", "Nicht vertrauter Tap — auf „Vertrauen“ klicken, um ihn zu erlauben"],
        ["Tooltip.Trust"] = ["brew trust — lets Homebrew load this tap", "brew trust — autorise Homebrew à charger ce tap", "brew trust — permite a Homebrew cargar este tap", "brew trust — erlaubt Homebrew, diesen Tap zu laden"],

        // Réglages
        ["Settings.Monitoring"] = ["Update monitoring", "Surveillance des mises à jour", "Supervisión de actualizaciones", "Update-Überwachung"],
        ["Settings.MonitoringDesc"] = ["Periodically check for outdated packages in the background.", "Vérifier périodiquement les paquets obsolètes en arrière-plan.", "Comprobar periódicamente los paquetes obsoletos en segundo plano.", "Regelmäßig im Hintergrund nach veralteten Paketen suchen."],
        ["Settings.Frequency"] = ["Check frequency", "Fréquence de vérification", "Frecuencia de comprobación", "Prüfintervall"],
        ["Settings.FrequencyDesc"] = ["Interval between two automatic checks.", "Intervalle entre deux contrôles automatiques.", "Intervalo entre dos comprobaciones automáticas.", "Intervall zwischen zwei automatischen Prüfungen."],
        ["Freq.Hourly"] = ["Every hour", "Toutes les heures", "Cada hora", "Stündlich"],
        ["Freq.6h"] = ["Every 6 hours", "Toutes les 6 heures", "Cada 6 horas", "Alle 6 Stunden"],
        ["Freq.Daily"] = ["Once a day", "Une fois par jour", "Una vez al día", "Einmal täglich"],
        ["Settings.Notifications"] = ["System notifications", "Notifications système", "Notificaciones del sistema", "Systembenachrichtigungen"],
        ["Settings.NotificationsDesc"] = ["Notify via the notification center when new updates arrive.", "Prévenir via le centre de notifications quand de nouvelles mises à jour arrivent.", "Avisar mediante el centro de notificaciones cuando lleguen nuevas actualizaciones.", "Über die Mitteilungszentrale benachrichtigen, wenn neue Updates eintreffen."],
        ["Settings.TerminalShortcut"] = ["Terminal shortcut", "Raccourci du terminal", "Atajo del terminal", "Terminal-Kürzel"],
        ["Settings.TerminalShortcutDesc"] = ["Keyboard shortcut to open and focus the terminal. Click, then press a combination.", "Raccourci pour ouvrir et focaliser le terminal. Cliquez, puis appuyez sur une combinaison.", "Atajo para abrir y enfocar el terminal. Haz clic y pulsa una combinación.", "Kürzel zum Öffnen und Fokussieren des Terminals. Klicken, dann eine Kombination drücken."],
        ["Settings.PressKeys"] = ["Press keys… (Esc to cancel)", "Appuyez… (Échap pour annuler)", "Pulsa teclas… (Esc para cancelar)", "Tasten drücken… (Esc zum Abbrechen)"],
        ["Settings.TestNotif"] = ["Test a notification", "Tester une notification", "Probar una notificación", "Benachrichtigung testen"],
        ["Settings.TestNotifDesc"] = ["Send a sample system notification to check delivery works.", "Envoyer une notification système d'exemple pour vérifier qu'elles arrivent.", "Enviar una notificación del sistema de ejemplo para comprobar la entrega.", "Eine Beispiel-Benachrichtigung senden, um die Zustellung zu prüfen."],
        ["Settings.TestNotifBtn"] = ["Send test", "Envoyer un test", "Enviar prueba", "Test senden"],
        ["Settings.TestNotifSent"] = ["Test notification sent — check your notification center.", "Notification de test envoyée — regardez votre centre de notifications.", "Notificación de prueba enviada — revisa tu centro de notificaciones.", "Testbenachrichtigung gesendet — prüfe deine Mitteilungszentrale."],
        ["Terminal.Title"] = ["Terminal", "Terminal", "Terminal", "Terminal"],
        ["Terminal.Clear"] = ["Clear output", "Effacer la sortie", "Borrar salida", "Ausgabe löschen"],
        ["Terminal.Toggle"] = ["Show / hide the terminal", "Afficher / masquer le terminal", "Mostrar / ocultar el terminal", "Terminal ein-/ausblenden"],
        ["Terminal.Empty"] = ["Output from brew commands appears here. Type a command below.", "La sortie des commandes brew apparaît ici. Tapez une commande ci-dessous.", "La salida de los comandos brew aparece aquí. Escribe un comando abajo.", "Die Ausgabe der brew-Befehle erscheint hier. Gib unten einen Befehl ein."],
        ["Terminal.Prompt"] = ["e.g. install wget · info git · list --versions", "ex. install wget · info git · list --versions", "p. ej. install wget · info git · list --versions", "z. B. install wget · info git · list --versions"],
        ["Terminal.Invalid"] = ["Only brew subcommands are allowed (no shell operators).", "Seules les sous-commandes brew sont permises (pas d'opérateurs shell).", "Solo se permiten subcomandos de brew (sin operadores de shell).", "Nur brew-Unterbefehle erlaubt (keine Shell-Operatoren)."],
        ["Terminal.Done"] = ["Command finished.", "Commande terminée.", "Comando finalizado.", "Befehl abgeschlossen."],
        ["Terminal.Failed"] = ["Command failed (exit {0}).", "Échec de la commande (code {0}).", "El comando falló (código {0}).", "Befehl fehlgeschlagen (Code {0})."],
        ["Notif.TestTitle"] = ["Cocktails", "Cocktails", "Cocktails", "Cocktails"],
        ["Notif.TestBody"] = ["Notifications are working 🍸", "Les notifications fonctionnent 🍸", "Las notificaciones funcionan 🍸", "Benachrichtigungen funktionieren 🍸"],
        ["Settings.Background"] = ["Keep running in background", "Rester actif en arrière-plan", "Seguir en segundo plano", "Im Hintergrund weiterlaufen"],
        ["Settings.BackgroundDesc"] = ["Closing the window hides the app (it keeps running via the menu bar icon). Otherwise, closing quits.", "Fermer la fenêtre masque l'app (elle continue en tâche de fond, via l'icône de la barre de menu). Sinon, fermer quitte l'app.", "Cerrar la ventana oculta la app (sigue en segundo plano, vía el icono de la barra de menús). Si no, cerrar sale de la app.", "Das Schließen des Fensters blendet die App aus (sie läuft über das Menüleistensymbol weiter). Andernfalls beendet Schließen die App."],
        ["Settings.Confirm"] = ["Confirm before uninstalling", "Confirmer avant désinstallation", "Confirmar antes de desinstalar", "Vor Deinstallation bestätigen"],
        ["Settings.ConfirmDesc"] = ["Ask for confirmation for irreversible actions.", "Demander une confirmation pour les actions irréversibles.", "Pedir confirmación para las acciones irreversibles.", "Bei unumkehrbaren Aktionen um Bestätigung bitten."],
        ["Settings.Analytics"] = ["Anonymous Homebrew analytics", "Statistiques anonymes Homebrew", "Estadísticas anónimas de Homebrew", "Anonyme Homebrew-Statistiken"],
        ["Settings.AnalyticsDesc"] = ["Homebrew telemetry (brew analytics). Changed directly in brew.", "Télémétrie de Homebrew (brew analytics). Modifiée directement dans brew.", "Telemetría de Homebrew (brew analytics). Se cambia directamente en brew.", "Homebrew-Telemetrie (brew analytics). Wird direkt in brew geändert."],
        ["Settings.Language"] = ["Language", "Langue", "Idioma", "Sprache"],
        ["Settings.LanguageDesc"] = ["Interface language.", "Langue de l'interface.", "Idioma de la interfaz.", "Sprache der Oberfläche."],
        ["Lang.System"] = ["System", "Système", "Sistema", "System"],
        ["Settings.HomebrewSection"] = ["HOMEBREW", "HOMEBREW", "HOMEBREW", "HOMEBREW"],
        ["Settings.Version"] = ["Version", "Version", "Versión", "Version"],
        ["Settings.Prefix"] = ["Prefix", "Préfixe", "Prefijo", "Präfix"],
        ["Settings.Cache"] = ["Cache", "Cache", "Caché", "Cache"],

        // Aide
        ["Help.Intro"] = ["Cocktails — graphical interface for Homebrew. Available keyboard shortcuts (⌘ = Command key):", "Cocktails — interface graphique pour Homebrew. Raccourcis clavier disponibles (⌘ = touche Commande) :", "Cocktails — interfaz gráfica para Homebrew. Atajos de teclado disponibles (⌘ = tecla Comando):", "Cocktails — grafische Oberfläche für Homebrew. Verfügbare Tastenkürzel (⌘ = Befehlstaste):"],
        ["Help.GroupNav"] = ["Navigation", "Navigation", "Navegación", "Navigation"],
        ["Help.GroupWindow"] = ["Window", "Fenêtre", "Ventana", "Fenster"],
        ["Help.GroupSearch"] = ["Search & filters", "Recherche & filtres", "Búsqueda y filtros", "Suche & Filter"],
        ["Help.JumpTabs"] = ["Jump to a tab (Installed, Search, Updates…)", "Aller à un onglet (Installés, Rechercher, Mises à jour…)", "Ir a una pestaña (Instalados, Buscar, Actualizaciones…)", "Zu einem Tab springen (Installiert, Suche, Updates…)"],
        ["Help.SwitchZone"] = ["Switch focus between the menu and the grid", "Basculer le focus entre le menu et la grille", "Cambiar el foco entre el menú y la cuadrícula", "Fokus zwischen Menü und Raster wechseln"],
        ["Help.GridArrows"] = ["Move within the grid (rows and columns)", "Se déplacer dans la grille (rangées et colonnes)", "Moverse por la cuadrícula (filas y columnas)", "Im Raster bewegen (Zeilen und Spalten)"],
        ["Help.ToggleCheck"] = ["Tick / untick the focused tile", "Cocher / décocher la tuile focalisée", "Marcar / desmarcar la tarjeta enfocada", "Fokussierte Kachel an-/abwählen"],
        ["Help.FocusFilter"] = ["Focus the filter / search field", "Placer le curseur dans le filtre / la recherche", "Enfocar el filtro / la búsqueda", "Filter- / Suchfeld fokussieren"],
        ["Help.ToggleTerminal"] = ["Open the terminal and type (again to close)", "Ouvrir le terminal et écrire (à nouveau pour fermer)", "Abrir el terminal y escribir (de nuevo para cerrar)", "Terminal öffnen und tippen (erneut zum Schließen)"],
        ["Help.GroupFilters"] = ["Filters", "Filtres", "Filtros", "Filter"],
        ["Help.FilterRoots"] = ["Toggle « roots only »", "Basculer « racines seulement »", "Alternar «solo raíces»", "„Nur Wurzeln“ umschalten"],
        ["Help.FilterKind"] = ["All / Formulae / Casks", "Tout / Formulae / Casks", "Todo / Formulae / Casks", "Alle / Formulae / Casks"],
        ["Help.RefreshList"] = ["Refresh the list", "Rafraîchir la liste", "Actualizar la lista", "Liste aktualisieren"],
        ["Help.OpenSettings"] = ["Open Settings", "Ouvrir les Réglages", "Abrir Ajustes", "Einstellungen öffnen"],
        ["Help.OpenHelp"] = ["Open this help", "Ouvrir cette aide", "Abrir esta ayuda", "Diese Hilfe öffnen"],
        ["Help.BrowseList"] = ["Browse the selected list", "Parcourir la liste sélectionnée", "Recorrer la lista seleccionada", "Durch die ausgewählte Liste blättern"],
        ["Help.HideWindow"] = ["Hide the window (the app stays in the background)", "Masquer la fenêtre (l'app reste en arrière-plan)", "Ocultar la ventana (la app sigue en segundo plano)", "Fenster ausblenden (die App bleibt im Hintergrund)"],
        ["Help.MinimizeWindow"] = ["Minimize the window", "Réduire la fenêtre", "Minimizar la ventana", "Fenster minimieren"],
        ["Help.Quit"] = ["Quit Cocktails", "Quitter Cocktails", "Salir de Cocktails", "Cocktails beenden"],
        ["Help.LaunchSearch"] = ["Run the search (in the Search field)", "Lancer la recherche (dans le champ Rechercher)", "Ejecutar la búsqueda (en el campo Buscar)", "Suche starten (im Suchfeld)"],
        ["Help.FilterLive"] = ["Filter the list live (Filter field)", "Filtrer la liste en direct (champ Filtrer)", "Filtrar la lista en vivo (campo Filtrar)", "Liste live filtern (Filterfeld)"],
        ["Help.KeyTyping"] = ["Typing", "Saisie", "Escritura", "Eingabe"],
        ["Help.BatchSection"] = ["BATCH OPERATIONS", "OPÉRATIONS PAR LOT", "OPERACIONES POR LOTES", "STAPELVERARBEITUNG"],
        ["Help.Batch1"] = ["Tick several rows in “Installed” or “Updates” using the checkboxes.", "Cochez plusieurs lignes dans « Installés » ou « Mises à jour » à l'aide des cases.", "Marca varias filas en «Instalados» o «Actualizaciones» con las casillas.", "Mehrere Zeilen in „Installiert“ oder „Updates“ per Kontrollkästchen markieren."],
        ["Help.Batch2"] = ["An action bar then appears atop the list with the number selected.", "Une barre d'actions apparaît alors en haut de la liste avec le nombre sélectionné.", "Aparece entonces una barra de acciones arriba de la lista con el número seleccionado.", "Oben in der Liste erscheint dann eine Aktionsleiste mit der Anzahl der Auswahl."],
        ["Help.Batch3"] = ["“Update selection” runs brew upgrade on all ticked rows.", "« Mettre à jour la sélection » lance brew upgrade sur toutes les lignes cochées.", "«Actualizar selección» ejecuta brew upgrade en todas las filas marcadas.", "„Auswahl aktualisieren“ führt brew upgrade für alle markierten Zeilen aus."],
        ["Help.Batch4"] = ["“Uninstall selection” chains the uninstalls (confirmation first).", "« Désinstaller la sélection » enchaîne les désinstallations (confirmation d'abord).", "«Desinstalar selección» encadena las desinstalaciones (con confirmación previa).", "„Auswahl deinstallieren“ verkettet die Deinstallationen (zuerst Bestätigung)."],
        ["Help.Batch5"] = ["“Uncheck all” resets the selection without running anything.", "« Tout décocher » remet la sélection à zéro sans rien lancer.", "«Desmarcar todo» reinicia la selección sin ejecutar nada.", "„Alle abwählen“ setzt die Auswahl zurück, ohne etwas auszuführen."],

        // Barre de menu (tray)
        ["Tray.Open"] = ["Open Cocktails", "Ouvrir Cocktails", "Abrir Cocktails", "Cocktails öffnen"],
        ["Tray.Search"] = ["Search…", "Rechercher…", "Buscar…", "Suchen…"],
        ["Tray.CheckNow"] = ["Check now", "Vérifier maintenant", "Comprobar ahora", "Jetzt prüfen"],
        ["Tray.Quit"] = ["Quit Cocktails", "Quitter Cocktails", "Salir de Cocktails", "Cocktails beenden"],
        ["Tray.UpdatesCount"] = ["Updates ({0})", "Mises à jour ({0})", "Actualizaciones ({0})", "Updates ({0})"],

        // Messages d'état / confirmations (view models)
        ["Status.Ready"] = ["Ready.", "Prêt.", "Listo.", "Bereit."],
        ["Status.Settings"] = ["Settings.", "Réglages.", "Ajustes.", "Einstellungen."],
        ["Status.MaintReady"] = ["Cleanup & diagnostics.", "Nettoyage & diagnostic.", "Limpieza y diagnóstico.", "Bereinigung & Diagnose."],
        ["Status.LoadingInstalled"] = ["Loading installed packages…", "Chargement des packages installés…", "Cargando paquetes instalados…", "Installierte Pakete werden geladen…"],
        ["Status.InstalledCount"] = ["{0} package(s) installed.", "{0} package(s) installé(s).", "{0} paquete(s) instalado(s).", "{0} Paket(e) installiert."],
        ["Status.SearchResults"] = ["{0} result(s) — {1} already installed.", "{0} résultat(s) — {1} déjà installé(s).", "{0} resultado(s) — {1} ya instalado(s).", "{0} Ergebnis(se) — {1} bereits installiert."],
        ["Status.Searching"] = ["Searching…", "Recherche…", "Buscando…", "Suche läuft…"],
        ["Status.LoadingUpdates"] = ["Checking for updates…", "Recherche des mises à jour…", "Buscando actualizaciones…", "Suche nach Updates…"],
        ["Status.AllUpToDate"] = ["Everything is up to date.", "Tout est à jour.", "Todo está actualizado.", "Alles ist aktuell."],
        ["Status.UpdatesAvailable"] = ["{0} update(s) available.", "{0} mise(s) à jour disponible(s).", "{0} actualización(es) disponible(s).", "{0} Update(s) verfügbar."],
        ["Status.IndexUpdating"] = ["Refreshing the index (brew update)…", "Actualisation de l'index (brew update)…", "Actualizando el índice (brew update)…", "Index wird aktualisiert (brew update)…"],
        ["Status.IndexUpToDate"] = ["Index up to date — everything is up to date.", "Index à jour — tout est à jour.", "Índice actualizado — todo está al día.", "Index aktuell — alles ist auf dem neuesten Stand."],
        ["Status.IndexUpdatesAvailable"] = ["Index up to date — {0} update(s) available.", "Index à jour — {0} mise(s) à jour disponible(s).", "Índice actualizado — {0} actualización(es) disponible(s).", "Index aktuell — {0} Update(s) verfügbar."],
        ["Status.Installing"] = ["Installing “{0}”…", "Installation de « {0} »…", "Instalando «{0}»…", "„{0}“ wird installiert…"],
        ["Status.Installed"] = ["“{0}” installed.", "« {0} » installé.", "«{0}» instalado.", "„{0}“ installiert."],
        ["Status.Uninstalling"] = ["Uninstalling “{0}”…", "Désinstallation de « {0} »…", "Desinstalando «{0}»…", "„{0}“ wird deinstalliert…"],
        ["Status.Uninstalled"] = ["“{0}” uninstalled.", "« {0} » désinstallé.", "«{0}» desinstalado.", "„{0}“ deinstalliert."],
        ["Status.Reinstalling"] = ["Reinstalling “{0}”…", "Réinstallation de « {0} »…", "Reinstalando «{0}»…", "„{0}“ wird neu installiert…"],
        ["Status.Reinstalled"] = ["“{0}” reinstalled.", "« {0} » réinstallé.", "«{0}» reinstalado.", "„{0}“ neu installiert."],
        ["Status.Upgrading"] = ["Updating “{0}”…", "Mise à jour de « {0} »…", "Actualizando «{0}»…", "„{0}“ wird aktualisiert…"],
        ["Status.Upgraded"] = ["“{0}” up to date.", "« {0} » à jour.", "«{0}» actualizado.", "„{0}“ aktuell."],
        ["Status.UpgradingAll"] = ["Updating all packages…", "Mise à jour de tous les packages…", "Actualizando todos los paquetes…", "Alle Pakete werden aktualisiert…"],
        ["Status.AllUpgraded"] = ["All packages are up to date.", "Tous les packages sont à jour.", "Todos los paquetes están actualizados.", "Alle Pakete sind aktuell."],
        ["Status.Pinning"] = ["Pinning “{0}”…", "Épinglage de « {0} »…", "Fijando «{0}»…", "„{0}“ wird angeheftet…"],
        ["Status.Unpinning"] = ["Unpinning “{0}”…", "Désépinglage de « {0} »…", "Desfijando «{0}»…", "„{0}“ wird gelöst…"],
        ["Status.Pinned"] = ["“{0}” pinned.", "« {0} » épinglé.", "«{0}» fijado.", "„{0}“ angeheftet."],
        ["Status.Unpinned"] = ["“{0}” unpinned.", "« {0} » désépinglé.", "«{0}» desfijado.", "„{0}“ gelöst."],
        ["Status.BatchUninstalling"] = ["Uninstalling {0} package(s)…", "Désinstallation de {0} paquet(s)…", "Desinstalando {0} paquete(s)…", "{0} Paket(e) werden deinstalliert…"],
        ["Status.BatchUninstallProgress"] = ["Uninstalling… ({0}/{1})", "Désinstallation… ({0}/{1})", "Desinstalando… ({0}/{1})", "Deinstallation… ({0}/{1})"],
        ["Status.BatchUninstalled"] = ["{0} package(s) uninstalled.", "{0} paquet(s) désinstallé(s).", "{0} paquete(s) desinstalado(s).", "{0} Paket(e) deinstalliert."],
        ["Status.BatchReinstalling"] = ["Reinstalling {0} package(s)…", "Réinstallation de {0} paquet(s)…", "Reinstalando {0} paquete(s)…", "{0} Paket(e) werden neu installiert…"],
        ["Status.BatchReinstallProgress"] = ["Reinstalling… ({0}/{1})", "Réinstallation… ({0}/{1})", "Reinstalando… ({0}/{1})", "Neuinstallation… ({0}/{1})"],
        ["Status.BatchReinstalled"] = ["{0} package(s) reinstalled.", "{0} paquet(s) réinstallé(s).", "{0} paquete(s) reinstalado(s).", "{0} Paket(e) neu installiert."],
        ["Status.BatchUpgrading"] = ["Updating {0} package(s)…", "Mise à jour de {0} paquet(s)…", "Actualizando {0} paquete(s)…", "{0} Paket(e) werden aktualisiert…"],
        ["Status.BatchUpgradeProgress"] = ["Updating… ({0}/{1})", "Mise à jour… ({0}/{1})", "Actualizando… ({0}/{1})", "Aktualisierung… ({0}/{1})"],
        ["Status.BatchUpgraded"] = ["{0} package(s) up to date.", "{0} paquet(s) à jour.", "{0} paquete(s) actualizado(s).", "{0} Paket(e) aktuell."],
        ["Status.CleanupRunning"] = ["Cleanup (brew cleanup)…", "Nettoyage (brew cleanup)…", "Limpieza (brew cleanup)…", "Bereinigung (brew cleanup)…"],
        ["Status.AutoremoveRunning"] = ["Removing orphaned dependencies…", "Suppression des dépendances orphelines…", "Eliminando dependencias huérfanas…", "Verwaiste Abhängigkeiten werden entfernt…"],
        ["Status.DoctorRunning"] = ["Diagnostics (brew doctor)…", "Diagnostic (brew doctor)…", "Diagnóstico (brew doctor)…", "Diagnose (brew doctor)…"],
        ["Status.MissingChecking"] = ["Checking missing dependencies…", "Vérification des dépendances manquantes…", "Comprobando dependencias faltantes…", "Fehlende Abhängigkeiten werden geprüft…"],
        ["Status.MissingNone"] = ["No missing dependency — everything is complete.", "Aucune dépendance manquante — tout est complet.", "Ninguna dependencia faltante — todo está completo.", "Keine fehlende Abhängigkeit — alles vollständig."],
        ["Status.MissingSome"] = ["{0} formula(e) with missing dependencies.", "{0} formule(s) avec des dépendances manquantes.", "{0} fórmula(s) con dependencias faltantes.", "{0} Formel(n) mit fehlenden Abhängigkeiten."],
        ["Status.BrewfileExporting"] = ["Exporting the Brewfile…", "Export du Brewfile…", "Exportando el Brewfile…", "Brewfile wird exportiert…"],
        ["Status.BrewfileExported"] = ["Brewfile exported.", "Brewfile exporté.", "Brewfile exportado.", "Brewfile exportiert."],
        ["Status.BrewfileImporting"] = ["Importing the Brewfile…", "Import du Brewfile…", "Importando el Brewfile…", "Brewfile wird importiert…"],
        ["Status.BrewfileImported"] = ["Brewfile imported.", "Brewfile importé.", "Brewfile importado.", "Brewfile importiert."],
        ["Status.LoadingServices"] = ["Loading services…", "Chargement des services…", "Cargando servicios…", "Dienste werden geladen…"],
        ["Status.ServicesCount"] = ["{0} service(s).", "{0} service(s).", "{0} servicio(s).", "{0} Dienst(e)."],
        ["Status.ServiceRunning"] = ["{0} of “{1}”…", "{0} de « {1} »…", "{0} de «{1}»…", "{0} von „{1}“…"],
        ["Status.ServiceDone"] = ["“{0}” — {1} done.", "« {0} » — {1} effectué.", "«{0}» — {1} completado.", "„{0}“ — {1} abgeschlossen."],
        ["Verb.Start"] = ["Start", "Démarrage", "Inicio", "Start"],
        ["Verb.Stop"] = ["Stop", "Arrêt", "Detención", "Stopp"],
        ["Verb.Restart"] = ["Restart", "Redémarrage", "Reinicio", "Neustart"],
        ["Status.LoadingTaps"] = ["Loading taps…", "Chargement des taps…", "Cargando taps…", "Taps werden geladen…"],
        ["Status.TapsCount"] = ["{0} tap(s).", "{0} tap(s).", "{0} tap(s).", "{0} Tap(s)."],
        ["Status.TapInvalid"] = ["Invalid tap name (expected: user/repo).", "Nom de tap invalide (attendu : utilisateur/dépôt).", "Nombre de tap no válido (esperado: usuario/repo).", "Ungültiger Tap-Name (erwartet: Benutzer/Repo)."],
        ["Status.TapAdding"] = ["Adding tap “{0}”…", "Ajout du tap « {0} »…", "Añadiendo tap «{0}»…", "Tap „{0}“ wird hinzugefügt…"],
        ["Status.TapAdded"] = ["Tap “{0}” added.", "Tap « {0} » ajouté.", "Tap «{0}» añadido.", "Tap „{0}“ hinzugefügt."],
        ["Status.TapRemoving"] = ["Removing tap “{0}”…", "Retrait du tap « {0} »…", "Quitando tap «{0}»…", "Tap „{0}“ wird entfernt…"],
        ["Status.TapRemoved"] = ["Tap “{0}” removed.", "Tap « {0} » retiré.", "Tap «{0}» quitado.", "Tap „{0}“ entfernt."],
        ["Status.TapTrusting"] = ["Trusting “{0}”…", "Confiance accordée à « {0} »…", "Confiando en «{0}»…", "Vertrauen für „{0}“…"],
        ["Status.TapTrusted"] = ["“{0}” approved.", "« {0} » approuvé.", "«{0}» aprobado.", "„{0}“ genehmigt."],
        ["Status.LoadingConfig"] = ["Reading Homebrew configuration…", "Lecture de la configuration Homebrew…", "Leyendo la configuración de Homebrew…", "Homebrew-Konfiguration wird gelesen…"],
        ["Status.AnalyticsOn"] = ["Homebrew analytics enabled.", "Statistiques Homebrew activées.", "Estadísticas de Homebrew activadas.", "Homebrew-Statistiken aktiviert."],
        ["Status.AnalyticsOff"] = ["Homebrew analytics disabled.", "Statistiques Homebrew désactivées.", "Estadísticas de Homebrew desactivadas.", "Homebrew-Statistiken deaktiviert."],
        ["Error.Brew"] = ["brew error: {0}", "Erreur brew : {0}", "Error de brew: {0}", "brew-Fehler: {0}"],
        ["Error.Generic"] = ["Error: {0}", "Erreur : {0}", "Error: {0}", "Fehler: {0}"],

        // Confirmations
        ["Confirm.UninstallTitle"] = ["Uninstall this package?", "Désinstaller ce paquet ?", "¿Desinstalar este paquete?", "Dieses Paket deinstallieren?"],
        ["Confirm.UninstallMsg"] = ["Uninstalling “{0}” is irreversible. Packages depending on it may stop working.", "La désinstallation de « {0} » est irréversible. Les paquets qui en dépendent pourraient cesser de fonctionner.", "Desinstalar «{0}» es irreversible. Los paquetes que dependen de él podrían dejar de funcionar.", "Die Deinstallation von „{0}“ ist unumkehrbar. Abhängige Pakete funktionieren möglicherweise nicht mehr."],
        ["Confirm.BatchUninstallTitle"] = ["Uninstall {0} package(s)?", "Désinstaller {0} paquet(s) ?", "¿Desinstalar {0} paquete(s)?", "{0} Paket(e) deinstallieren?"],
        ["Confirm.BatchUninstallMsg"] = ["These packages will be uninstalled: {0}. Irreversible; packages depending on them may stop working.", "Ces paquets vont être désinstallés : {0}. Opération irréversible ; les paquets qui en dépendent pourraient cesser de fonctionner.", "Estos paquetes se desinstalarán: {0}. Irreversible; los paquetes que dependen de ellos podrían dejar de funcionar.", "Diese Pakete werden deinstalliert: {0}. Unumkehrbar; abhängige Pakete funktionieren möglicherweise nicht mehr."],
        ["Confirm.UninstallBtn"] = ["Uninstall", "Désinstaller", "Desinstalar", "Deinstallieren"],
        ["Confirm.BatchUninstallBtn"] = ["Uninstall all", "Tout désinstaller", "Desinstalar todo", "Alle deinstallieren"],
        ["Confirm.AutoremoveTitle"] = ["Remove orphaned dependencies?", "Retirer les dépendances orphelines ?", "¿Quitar las dependencias huérfanas?", "Verwaiste Abhängigkeiten entfernen?"],
        ["Confirm.AutoremoveMsg"] = ["“brew autoremove” will remove formulae installed automatically and no longer needed.", "« brew autoremove » supprimera les formulae installées automatiquement et devenues inutiles.", "«brew autoremove» eliminará las fórmulas instaladas automáticamente y ya innecesarias.", "„brew autoremove“ entfernt automatisch installierte, nicht mehr benötigte Formeln."],
        ["Confirm.AutoremoveBtn"] = ["Remove", "Retirer", "Quitar", "Entfernen"],
        ["Confirm.ImportTitle"] = ["Import this Brewfile?", "Importer ce Brewfile ?", "¿Importar este Brewfile?", "Dieses Brewfile importieren?"],
        ["Confirm.ImportMsg"] = ["Missing entries (taps, formulae, casks) will be installed. This can take a while.", "Les entrées manquantes (taps, formulae, casks) seront installées. Cela peut être long.", "Se instalarán las entradas que falten (taps, fórmulas, casks). Puede tardar.", "Fehlende Einträge (Taps, Formeln, Casks) werden installiert. Das kann dauern."],
        ["Confirm.ImportBtn"] = ["Import", "Importer", "Importar", "Importieren"],
        ["Confirm.RemoveTapTitle"] = ["Remove this tap?", "Retirer ce tap ?", "¿Quitar este tap?", "Diesen Tap entfernen?"],
        ["Confirm.RemoveTapMsg"] = ["“{0}” will be removed. Its formulae/casks will no longer be available to install.", "« {0} » sera retiré. Ses formules/casks ne seront plus disponibles à l'installation.", "«{0}» se quitará. Sus fórmulas/casks ya no estarán disponibles para instalar.", "„{0}“ wird entfernt. Seine Formeln/Casks stehen nicht mehr zur Installation bereit."],
        ["Confirm.RemoveTapBtn"] = ["Remove", "Retirer", "Quitar", "Entfernen"],

        // Notifications
        ["Notif.OneUpdate"] = ["Update available: {0}", "Mise à jour disponible : {0}", "Actualización disponible: {0}", "Update verfügbar: {0}"],
        ["Notif.ManyUpdates"] = ["{0} new updates: {1}", "{0} nouvelles mises à jour : {1}", "{0} nuevas actualizaciones: {1}", "{0} neue Updates: {1}"],
    };
}
