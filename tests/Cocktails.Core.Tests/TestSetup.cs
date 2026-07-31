using System.Runtime.CompilerServices;
using Cocktails.Localization;

// Le Localizer est un singleton mutable : on sérialise les tests pour éviter les courses
// (un test qui change la langue perturberait les assertions localisées d'un autre).
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Cocktails.Core.Tests;

/// <summary>
/// Fixe la langue de l'interface en français pour tous les tests, afin que les assertions
/// sur les messages d'état (localisés) soient déterministes quelle que soit la culture de
/// la machine de test.
/// </summary>
internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init() => Localizer.Instance.SetLanguage(AppLanguage.French);
}
