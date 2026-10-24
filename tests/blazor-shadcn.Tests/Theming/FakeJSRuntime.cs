using System.Text.Json;
using Microsoft.JSInterop;

namespace BlazorShadcn.Tests.Theming;

/// <summary>
/// Hand-rolled <see cref="IJSRuntime"/> for theme tests. Records every call (including calls made on the
/// returned module) and answers with scripted results instead of a real browser.
/// </summary>
internal sealed class FakeJSRuntime : IJSRuntime
{
    /// <summary>Every interop call in order: <c>import</c> first, then module calls such as <c>init</c>.</summary>
    public List<JSCall> Calls { get; } = [];

    /// <summary>Mode returned by the scripted <c>init</c> call.</summary>
    public string InitMode { get; set; } = "system";

    /// <summary>Resolved scheme returned by the scripted <c>init</c> call.</summary>
    public string InitResolved { get; set; } = "light";

    /// <summary>Resolved scheme returned by the scripted <c>setMode</c> call.</summary>
    public string SetModeResolved { get; set; } = "light";

    /// <summary>When set, the <c>dispose</c> call throws this exception (e.g. a disconnected circuit).</summary>
    public Exception? DisposeException { get; set; }

    /// <summary>When true, the <c>import</c> call throws <see cref="InvalidOperationException"/>, as during prerendering.</summary>
    public bool ThrowOnImport { get; set; }

    /// <summary>The module handed out by the last successful <c>import</c>, or null.</summary>
    public FakeModule? Module { get; private set; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add(new JSCall(identifier, args));

        if (identifier != "import")
        {
            return ValueTask.FromException<TValue>(
                new InvalidOperationException($"Unscripted runtime call '{identifier}'."));
        }

        if (ThrowOnImport)
        {
            return ValueTask.FromException<TValue>(
                new InvalidOperationException("JavaScript interop calls cannot be issued during prerendering."));
        }

        Module = new FakeModule(this);
        return ValueTask.FromResult((TValue)(object)Module);
    }

    /// <summary>One recorded interop call.</summary>
    /// <param name="Identifier">The JS function or module path name.</param>
    /// <param name="Args">Arguments passed with the call.</param>
    public sealed record JSCall(string Identifier, object?[]? Args);

    /// <summary>
    /// Stand-in for the <c>theme.js</c> module reference. Answers <c>init</c>, <c>setMode</c> and
    /// <c>dispose</c> from the owning runtime's scripted values.
    /// </summary>
    public sealed class FakeModule : IJSObjectReference
    {
        private readonly FakeJSRuntime _runtime;

        internal FakeModule(FakeJSRuntime runtime)
        {
            _runtime = runtime;
        }

        /// <summary>True once <see cref="DisposeAsync"/> has been awaited.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            _runtime.Calls.Add(new JSCall(identifier, args));

            try
            {
                return ValueTask.FromResult(Respond<TValue>(identifier));
            }
            catch (Exception ex)
            {
                return ValueTask.FromException<TValue>(ex);
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }

        private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

        private TValue Respond<TValue>(string identifier)
        {
            switch (identifier)
            {
                case "init":
                    // ThemeService deserializes the result into its internal ThemeState record. Blazor's interop
                    // uses web defaults (camelCase, case-insensitive), so the round trip mirrors that.
                    var state = new { mode = _runtime.InitMode, resolved = _runtime.InitResolved };
                    var json = JsonSerializer.Serialize(state, WebOptions);
                    return JsonSerializer.Deserialize<TValue>(json, WebOptions)!;

                case "setMode":
                    return (TValue)(object)_runtime.SetModeResolved;

                case "dispose":
                    if (_runtime.DisposeException is { } exception)
                    {
                        throw exception;
                    }

                    return default!;

                default:
                    throw new InvalidOperationException($"Unscripted module call '{identifier}'.");
            }
        }
    }
}
