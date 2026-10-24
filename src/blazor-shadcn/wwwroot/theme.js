// Color-scheme bridge for ThemeService. Toggles a class (default `dark`) on <html>, optionally sets
// `data-theme`, and persists the user's choice in localStorage. Kept dependency-free so it can be copied
// with the components.

const DARK_QUERY = '(prefers-color-scheme: dark)';
const DISABLE_TRANSITION_CSS = '* { -webkit-transition: none !important; transition: none !important; }';

const DEFAULTS = Object.freeze({
    storageKey: 'blazor-shadcn-theme',
    defaultMode: 'system',
    darkClass: 'dark',
    useColorSchemeAttribute: false,
    disableTransitionOnChange: true,
});

let media = null;
let listener = null;
let dotnetRef = null;
let mode = 'system';
let config = DEFAULTS;

function ensureMedia() {
    if (!media) {
        media = window.matchMedia(DARK_QUERY);
    }

    return media;
}

function normalize(value, fallback) {
    return value === 'light' || value === 'dark' || value === 'system' ? value : fallback;
}

// Merges the options sent from .NET with the defaults, so a partial object still works for direct JS callers.
function resolveOptions(options) {
    const source = options ?? {};

    return {
        storageKey: typeof source.storageKey === 'string' && source.storageKey.length > 0 ? source.storageKey : DEFAULTS.storageKey,
        defaultMode: normalize(source.defaultMode, DEFAULTS.defaultMode),
        darkClass: typeof source.darkClass === 'string' && source.darkClass.length > 0 ? source.darkClass : DEFAULTS.darkClass,
        useColorSchemeAttribute: source.useColorSchemeAttribute === true,
        disableTransitionOnChange: source.disableTransitionOnChange !== false,
    };
}

function readStoredMode(storageKey, fallback) {
    try {
        return normalize(localStorage.getItem(storageKey), fallback);
    } catch {
        // Storage can be unavailable (privacy modes, sandboxed frames); fall back to the configured default.
        return fallback;
    }
}

function resolve(current) {
    if (current === 'system') {
        return ensureMedia().matches ? 'dark' : 'light';
    }

    return current;
}

function apply(resolved) {
    const root = document.documentElement;
    const change = () => {
        root.classList.toggle(config.darkClass, resolved === 'dark');

        if (config.useColorSchemeAttribute) {
            root.setAttribute('data-theme', resolved);
        }
    };

    if (config.disableTransitionOnChange) {
        withoutTransitions(change);
    } else {
        change();
    }

    return resolved;
}

// Same approach as next-themes: inject a style that forbids transitions, apply the change, force a style
// recalculation so the browser commits it, then remove the style on the next tick.
function withoutTransitions(change) {
    const style = document.createElement('style');
    style.appendChild(document.createTextNode(DISABLE_TRANSITION_CSS));
    document.head.appendChild(style);

    change();

    // Reading a computed style forces the browser to flush the pending style changes while the style is still in place.
    window.getComputedStyle(document.body);

    setTimeout(() => style.remove(), 1);
}

/**
 * Reads the persisted mode, applies the matching class and starts listening for OS scheme changes.
 * Calling it again replaces the previous subscription.
 * @param {object} options Theme options: { storageKey, defaultMode, darkClass, useColorSchemeAttribute, disableTransitionOnChange }.
 *   Missing or invalid values fall back to the defaults.
 * @param {object} ref DotNetObjectReference whose OnSystemSchemeChanged(bool) receives OS changes.
 * @returns {{ mode: string, resolved: string }}
 */
export function init(options, ref) {
    dispose();

    config = resolveOptions(options);
    dotnetRef = ref;
    mode = readStoredMode(config.storageKey, config.defaultMode);
    const resolved = apply(resolve(mode));

    listener = (event) => {
        if (mode === 'system') {
            apply(event.matches ? 'dark' : 'light');
        }

        dotnetRef?.invokeMethodAsync('OnSystemSchemeChanged', event.matches);
    };

    ensureMedia().addEventListener('change', listener);

    return { mode, resolved };
}

/**
 * Persists the mode and re-applies the document class. 'system' is stored explicitly, not removed, so it
 * still wins over a non-system default passed to init().
 * @param {object} options Theme options, as for init(). Only storageKey and the class/attribute settings are used here.
 * @param {string} next 'light' | 'dark' | 'system'.
 * @returns {string} The resolved scheme, 'light' or 'dark'.
 */
export function setMode(options, next) {
    config = resolveOptions(options);
    mode = normalize(next, config.defaultMode);

    try {
        localStorage.setItem(config.storageKey, mode);
    } catch {
        // Storage unavailable: the class still applies for the current page view.
    }

    return apply(resolve(mode));
}

/** Unsubscribes from OS scheme changes and drops the .NET callback reference. */
export function dispose() {
    if (media && listener) {
        media.removeEventListener('change', listener);
    }

    listener = null;
    dotnetRef = null;
}
