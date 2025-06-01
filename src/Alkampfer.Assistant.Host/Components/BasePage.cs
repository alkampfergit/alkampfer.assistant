using System;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Alkampfer.Assistant.Host.Components;

public class BasePage : ComponentBase
{
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
}
