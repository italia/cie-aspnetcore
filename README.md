# AspNetCore Remote Authenticator for CIE3.0
This is a custom implementation of an AspNetCore RemoteAuthenticationHandler for CIE3.0 (a.k.a. the Italian 'Carta d'Identità Elettronica').
Since it's an Italian-only thing, there's no point in struggling with an english README, just italian from now on.

Lo scopo di questo progetto è quello di fornire uno strumento semplice ed immediato per integrare, in una WebApp sviluppata con AspNetCore MVC, i servizi di autenticazione di CIE3.0, automatizzando i flussi di login/logout, la gestione del protocollo SAML, la security e semplificando le attività di sviluppo e markup.

# Integrazione

La libreria viene distribuita sotto forma di pacchetto NuGet, installabile tramite il comando

`Install-Package CIE.AspNetCore.Authentication`

A questo punto è sufficiente, all'interno dello `Startup.cs`, aggiungere le seguenti righe:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews();
    services
        .AddAuthentication(o => {
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = CieDefaults.AuthenticationScheme;
        })
        .AddCie(Configuration, o => {
            o.LoadFromConfiguration(Configuration);
        })
        .AddCookie();
}
```

In questo modo vengono aggiunti i middleware necessari per la gestione delle richieste/risposte di login/logout da/verso l'identityProvider di CIE3.0.

Nella libreria è inclusa anche l'implementazione di un TagHelper per la renderizzazione (conforme alle specifiche) del pulsante "Entra con CIE".
Per renderizzare il pulsante è sufficiente aggiungere il seguente codice alla View Razor dove lo si desidera posizionare:

```razor
@using CIE.AspNetCore.Authentication
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, CIE.AspNetCore.Authentication
@{
	ViewData["Title"] = "Home Page";
}
<div class="text-center">
	<h1 class="display-4">Welcome</h1>
	<cie-button challenge-url="/home/login" size="Medium" class="text-left"></cie-button>
</div>
```

Il TagHelper `cie-button` si occuperà di generare automaticamente il codice HTML necessario per la renderizzazione del pulsante. 
Un esempio completo di webapp AspNetCore MVC che fa uso di questa libreria è presente all'interno di questo repository sotto la cartella `CIE.AspNetCore.Authentication/CIE.AspNetCore.WebApp`. Per utilizzarla è sufficiente configurare in `appsettings.json` i parametri `AssertionConsumerServiceIndex`, `AttributeConsumingServiceIndex`, `EntityId` e `Certificate` con quelli relativi al proprio metadata di test, e lanciare la webapp.

# Configurazione
E' possibile configurare la libreria leggendo le impostazioni da Configurazione, tramite il comando

```csharp
o.LoadFromConfiguration(Configuration);
```

In particolare è possibile aggiungere alla configurazione una sezione 'Cie' che ha il seguente formato

```json
  "Cie": {
    "Provider": {
      "Name": "CIE Test/PreProduzione",
      "OrganizationName": "CIE Test/PreProduzione",
      "OrganizationDisplayName": "CIE Test/PreProduzione",
      "OrganizationUrlMetadata": "https://preproduzione.idserver.servizicie.interno.gov.it/idp/shibboleth?Metadata",
      "OrganizationUrl": "https://www.interno.gov.it/it",
      "OrganizationLogoUrl": "",
      "SingleSignOnServiceUrl": "https://preproduzione.idserver.servizicie.interno.gov.it/idp/profile/SAML2/POST/SSO",
      "SingleSignOutServiceUrl": "https://preproduzione.idserver.servizicie.interno.gov.it/idp/profile/SAML2/POST/SLO",
      "Method": "Post",
      "Type": "StagingProvider",
      "SecurityLevel": 3
    },
    "Certificate": {
      "Source": "Store/Raw/File/None",
      "Store": {
        "Location": "CurrentUser",
        "Name": "My",
        "FindType": "FindBySubjectName",
        "FindValue": "HackDevelopers",
        "validOnly": false
      },
      "File": {
        "Path": "xxx.pfx",
        "Password": "xxx"
      },
      "Raw": {
        "Certificate": "test",
        "Password": "test"
      }
    },
    "EntityId": "https://entityID",
    "AssertionConsumerServiceIndex": 0,
    "AttributeConsumingServiceIndex": 0
  }
```
La configurazione del certificato del SP avviene specificando nel campo `Source` uno tra i valori `Store/File/Raw/None` (nel caso di `None` non verrà caricato un certificato durante lo startup, ma sarà necessario fornirne uno a runtime, tramite l'uso dei `CustomCieEvents`, che verranno presentati più nel dettaglio nella sezione successiva) e compilando opportunamente la sezione corrispondente al valore specificato. Le sezioni non usate (quelle cioè corrispondenti agli altri valori) potranno essere tranquillamente eliminate dal file di configurazione, dal momento che non verranno lette.

In alternativa, è possibile configurare tutte le suddette opzioni programmaticamente, dal metodo `AddCie(options => ...)`.
Gli endpoint di callback per le attività di signin e signout sono impostati di default, rispettivamente, a `/signin-cie` e `/signout-cie`, ma laddove fosse necessario modificare queste impostazioni, è possibile sovrascriverle (sia da configurazione che da codice) reimpostando le options `CallbackPath` e `RemoteSignOutPath`.

# Punti d'estensione
E' possibile intercettare le varie fasi di esecuzione del RemoteAuthenticator, effettuando l'override degli eventi esposti dalla option Events, ed eventualmente utilizzare la DependencyInjection per avere a disposizione i vari servizi configurati nella webapp.
Questo torna utile sia in fase di inspection delle request e delle response da/verso l'identity provider di CIE3.0, sia per personalizzare, a runtime, alcuni parametri per la generazione della richiesta SAML (ad esempio nel caso in cui si voglia implementare la multitenancy). Ad esempio

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllersWithViews();
    services
        .AddAuthentication(o => {
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = CieDefaults.AuthenticationScheme;
        })
        .AddCie(Configuration, o => {
            o.Events.OnTokenCreating = async (s) => await s.HttpContext.RequestServices.GetRequiredService<CustomCieEvents>().TokenCreating(s);
	    o.Events.OnAuthenticationSuccess = async (s) => await s.HttpContext.RequestServices.GetRequiredService<CustomCieEvents>().AuthenticationSuccess(s);
            o.LoadFromConfiguration(Configuration);
        })
        .AddCookie();
    services.AddScoped<CustomCieEvents>();
}

.....

public class CustomCieEvents : CieEvents
{
    private readonly IMyService _myService;
    public CustomCieEvents(IMyService myService)
    {
        _myService = myService;
    }

    public override Task TokenCreating(SecurityTokenCreatingContext context)
    {
        var customConfig = _myService.ReadMyCustomConfigFromWhereverYouWant();
        context.TokenOptions.EntityId = customConfig.EntityId;
        context.TokenOptions.AssertionConsumerServiceIndex = customConfig.AssertionConsumerServiceIndex;
        context.TokenOptions.AttributeConsumingServiceIndex = customConfig.AttributeConsumingServiceIndex;
        context.TokenOptions.Certificate = customConfig.Certificate;

        return base.TokenCreating(context);
    }
    
    public override Task AuthenticationSuccess(AuthenticationSuccessContext context)
    {
        var principal = context.Principal;
	
	// Recupero dati provenienti da Cie da ClaimsPrincipal
        var name = principal.FindFirst(CieClaimTypes.Name);
        var familyName = principal.FindFirst(CieClaimTypes.FamilyName);
        var email = principal.FindFirst(CieClaimTypes.Email);
        var dateOfBirth = principal.FindFirst(CieClaimTypes.DateOfBirth);
	
        return base.AuthenticationSuccess(context);
    }
}
```

# Compliance
La libreria è stata oggetto di collaudo da parte dell'Istituto Poligrafico e Zecca dello Stato, ha superato tutti i test di [spid-saml-check](https://github.com/italia/spid-saml-check) ed è compliant con le direttive specificate nel manuale operativo di CIE.

# Authors
* [Daniele Giallonardo](https://github.com/danielegiallonardo) (maintainer) - [Stefano Mostarda](https://github.com/sm15455)
