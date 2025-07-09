# Dependency Injection Implementierungsplan
## Überblick
Dieser Plan beschreibt die schrittweise Umstellung des cdx-enrich-Projekts auf Dependency Injection. Die Umstellung wird in logisch aufeinander aufbauenden Schritten durchgeführt, um den Code modular, testbar und wartbar zu gestalten.
## Checkliste
### 1. Grundlagen einrichten
- [ ] **Paketabhängigkeiten hinzufügen**
  - : Microsoft.Extensions.DependencyInjection und Microsoft.Extensions.Logging.Abstractions installieren `Program.cs`
  - `*.csproj`: Sicherstellen, dass die neuesten Versionen der Pakete verwendet werden
  - Ausführen: `dotnet add package Microsoft.Extensions.DependencyInjection`
  - Ausführen: `dotnet add package Microsoft.Extensions.Logging.Abstractions`

### 2. DI-Container einrichten
- [ ] **ServiceCollection in Program.cs erstellen**
  - : Methode `ConfigureServices` zur Konfiguration der Dienste hinzufügen `Program.cs`
  - : `BuildServiceProvider()` implementieren `Program.cs`
  - Detaillierte Schritte:
    1. Eine private Methode `ConfigureServices` erstellen, die eine `ServiceCollection` zurückgibt
    2. In der `Main`-Methode den ServiceProvider erzeugen und für die Ausführung verwenden

### 3. Interfaces für bestehende Dienste erstellen
- [ ] **Interface für LicenseResolver erstellen**
  - `CdxEnrich.ClearlyDefined/Interfaces/ILicenseResolver.cs`: Interface definieren mit der `Resolve`-Methode
  - : Klasse anpassen, um Interface zu implementieren `CdxEnrich.ClearlyDefined/LicenseResolver.cs`
  - Detaillierte Schritte:
    1. Verzeichnis `CdxEnrich.ClearlyDefined/Interfaces` erstellen, falls nicht vorhanden
    2. Neues Interface `ILicenseResolver` mit der `Resolve`-Methode erstellen:

- [ ] **Interface für Lizenz-Regeln erstellen**
  - : Interface extrahieren (falls nicht bereits vorhanden) `CdxEnrich.ClearlyDefined/Rules/Interfaces/IResolveLicenseRule.cs`
  - Detaillierte Schritte:
    1. Sicherstellen, dass das `IResolveLicenseRule`-Interface alle notwendigen Methoden enthält
    2. Alle Implementierungen anpassen, um das Interface explizit zu implementieren

### 4. Action-Klassen für DI anpassen
- [ ] **Interfaces für Actions erstellen**
  - `CdxEnrich.Actions/Interfaces/IExecutableAction.cs`: Interface die Action-Ausführung
``` csharp
    public interface IExecutableAction
    {
        InputTuple Execute(InputTuple input);
    }
```
- `CdxEnrich.Actions/Interfaces/ICheckConfigAction.cs`: Interface für Konfigurationsprüfung
``` csharp
    public interface ICheckConfigAction
    {
        Result<ConfigRoot> CheckConfig(ConfigRoot config);
        Result<InputTuple> CheckBomAndConfigCombination(InputTuple inputs)
        {
            return Result.Ok(inputs);
        }
    }
```
- [ ] **ReplaceLicensesByUrl umstellen**
  - `CdxEnrich.Actions/ReplaceLicensesByUrl.cs`: Statische Methoden in Instanzmethoden umwandeln
  - `CdxEnrich.Actions/ReplaceLicensesByUrl.cs`: Konstruktor für Dependency Injection erstellen
  - Detaillierte Schritte:
    1. Logger als Abhängigkeit hinzufügen
    2. Statische Methoden `Execute` und `CheckConfig` in Instanzmethoden umwandeln
    3. In ServiceCollection registrieren mit `services.AddTransient<IExecutableAction, ReplaceLicensesByUrl>()`
    4. In ServiceCollection registrieren mit `services.AddTransient<ICheckConfigAction, ReplaceLicensesByUrl>()`

- [ ] **ReplaceLicenseByBomRef umstellen**
  - : Statische Methoden in Instanzmethoden umwandeln `CdxEnrich.Actions/ReplaceLicenseByBomRef.cs`
  - : Konstruktor für Dependency Injection erstellen `CdxEnrich.Actions/ReplaceLicenseByBomRef.cs`
  - Detaillierte Schritte:
    1. Logger als Abhängigkeit hinzufügen
    2. Statische Methoden `Execute` und `CheckConfig` in Instanzmethoden umwandeln
    3. In ServiceCollection registrieren mit `services.AddTransient<IExecutableAction, ReplaceLicenseByBomRef>()`
    4. In ServiceCollection registrieren mit `services.AddTransient<ICheckConfigAction, ReplaceLicenseByBomRef>()`

- [ ] **ReplaceLicenseByClearlyDefined umstellen**
  - : Statische Methoden in Instanzmethoden umwandeln `CdxEnrich.Actions/ReplaceLicenseByClearlyDefined.cs`
  - : Konstruktor für Dependency Injection erstellen `CdxEnrich.Actions/ReplaceLicenseByClearlyDefined.cs`
  - Abhängigkeit zu `ILicenseResolver` hinzufügen
  - Detaillierte Schritte:
    1. Logger und LicenseResolver als Abhängigkeiten hinzufügen
    2. Statische Methoden `Execute`, `CheckConfig` und `CheckBomAndConfigCombination` in Instanzmethoden umwandeln
    3. In ServiceCollection registrieren mit `services.AddTransient<IExecutableAction, ReplaceLicenseByClearlyDefined>()`
    4. In ServiceCollection registrieren mit `services.AddTransient<ICheckConfigAction, ReplaceLicenseByClearlyDefined>()`

### 5. Runner-Klasse umstellen
- [ ] **Runner in Instanz-Klasse umwandeln**
  - `CdxEnrich/Interfaces/IRunner.cs`: Interface für Runner erstellen
``` csharp
    public interface IRunner
    {
        int Enrich(string inputFilePath, CycloneDXFormatOption inputFormat, string outputFilePath, 
                  IEnumerable<string> configPaths, CycloneDXFormatOption outputFileFormat);
        Result<string> Enrich(string inputFileContent, CycloneDXFormat inputFormat, 
                             string configFileContent, CycloneDXFormat outputFileFormat);
    }
```
- : Statische Methoden in Instanzmethoden umwandeln `CdxEnrich/Runner.cs`
- : Konstruktor mit Abhängigkeiten zu Actions erstellen `CdxEnrich/Runner.cs`
- Detaillierte Schritte:
  1. Liste von `IExecutableAction` und `ICheckConfigAction`-Implementierungen als Abhängigkeit hinzufügen
  2. Statische Methoden in Instanzmethoden umwandeln
  3. In ServiceCollection registrieren mit `services.AddTransient<IRunner, Runner>()`

### 6. Logging-Infrastruktur
- [ ] **Logging-Framework integrieren**
  - : Logger-Konfiguration hinzufügen `Program.cs`
  - Sicherstellen, dass alle Klassen ILogger-Instanzen über DI erhalten
  - Detaillierte Schritte:
    1. In `ConfigureServices` Logging konfigurieren: `services.AddLogging(configure => configure.AddConsole())`
    2. Alle Nullchecks und NullLogger-Instanzen durch korrekte DI-Instanzen ersetzen

### 7. Factory-Klassen für komplexe Objekte
- [ ] **Factory für Regeln erstellen**
  - `CdxEnrich.ClearlyDefined/Rules/Factories/Interfaces/IResolveLicenseRuleFactory.cs`: Interface für Factory erstellen
``` csharp
    public interface IResolveLicenseRuleFactory
    {
        IEnumerable<IResolveLicenseRule> CreateRules();
    }
```
- `CdxEnrich.ClearlyDefined/Rules/Factories/ResolveLicenseRuleFactory.cs`: Factory für Regeln implementieren
- Detaillierte Schritte:
  1. Erstellen einer Factory-Klasse, die alle Regeln erzeugt
  2. Logger als Abhängigkeit für die Factory hinzufügen
  3. In ServiceCollection registrieren mit `services.AddTransient<IResolveLicenseRuleFactory, ResolveLicenseRuleFactory>()`
  4. `LicenseResolver` anpassen, um die Factory zu verwenden

### 8. Program.cs anpassen
- [ ] **Kommandozeilenverarbeitung mit DI verbinden**
  - : Host Builder und Kommandozeilen-Integration `Program.cs`
  - Sicherstellen, dass alle Dienste korrekt registriert sind
  - Detaillierte Schritte:
    1. `IRunner` über DI auflösen und für die Kommandozeilenverarbeitung verwenden
    2. Sicherstellen, dass alle benötigten Dienste in `ConfigureServices` registriert sind

### 9. Tests anpassen
- [ ] **Bestehende Tests an DI anpassen**
  - Test-Projekte aktualisieren, um Mock-Objekte zu verwenden
  - Sicherstellen, dass alle Tests mit der neuen DI-Struktur funktionieren
  - Detaillierte Schritte:
    1. Moq oder eine andere Mocking-Bibliothek einbinden, falls noch nicht vorhanden
    2. Test-Fixtures anpassen, um Mock-Objekte für Abhängigkeiten zu erstellen
    3. Tests aktualisieren, um die neuen Instanz-Methoden statt statischer Methoden zu verwenden

### 10. Dokumentation aktualisieren
- [ ] **README.md und Inline-Dokumentation aktualisieren**
  - : Beschreibung der neuen Architektur hinzufügen `README.md`
  - Inline-Dokumentation für neue Interfaces und DI-spezifische Komponenten
  - Detaillierte Schritte:
    1. Architekturübersicht für DI im README.md ergänzen
    2. Xml-Dokumentation für alle neuen Interfaces und Klassen hinzufügen
    3. Sicherstellen, dass alle Abhängigkeiten klar dokumentiert sind

### 11. Konfigurationshandling verbessern [optional]
- [ ] **Konfigurationsklasse erstellen**
  - `CdxEnrich.Config/Interfaces/IConfigurationService.cs`: Interface für Konfigurationsservice erstellen
``` csharp
    public interface IConfigurationService
    {
        Result<ConfigRoot> ParseConfig(string configContent);
    }
```
- `CdxEnrich.Config/ConfigurationService.cs`: Service zum Laden von Konfigurationen implementieren
- Detaillierte Schritte:
  1. Aktuelle statische Methoden aus `ConfigLoader` in eine Service-Klasse überführen
  2. IConfigurationService im Runner verwenden
  3. Logger als Abhängigkeit hinzufügen
  4. In ServiceCollection registrieren mit `services.AddTransient<IConfigurationService, ConfigurationService>()`

### 12. Serialisierung anpassen [optional]
- [ ] **BomSerialization als Service implementieren**
  - `CdxEnrich.Serialization/Interfaces/IBomSerializer.cs`: Interface für Serialisierung erstellen
``` csharp
    public interface IBomSerializer
    {
        Result<Bom> DeserializeBom(string content, CycloneDXFormat format);
        Result<string> SerializeBom(Bom bom, CycloneDXFormat format);
    }
```
- `CdxEnrich.Serialization/BomSerializer.cs`: Klasse für Serialisierung erstellen
- Detaillierte Schritte:
  1. Aktuelle statische Methoden aus `BomSerialization` in eine Service-Klasse überführen
  2. IBomSerializer im Runner verwenden
  3. Logger als Abhängigkeit hinzufügen
  4. In ServiceCollection registrieren mit `services.AddTransient<IBomSerializer, BomSerializer>()`
