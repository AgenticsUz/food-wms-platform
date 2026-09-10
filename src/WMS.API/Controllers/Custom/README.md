# Custom controllers

One controller per custom feature. Template:

```csharp
namespace WMS.API.Controllers.Custom;

[Route("api/custom/<feature>")]
[RequireFeature("custom.<feature>")]   // mandatory — the whole point of the folder
public class <Feature>Controller : BaseController
{
    // ...
}
```

Rules and the reasoning behind them: `WMS.Application/Features/Custom/README.md`.
Registry of what exists and for whom: `docs/CUSTOM_FEATURES.md`.
