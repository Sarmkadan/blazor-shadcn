// Color-scheme bridge for ThemeService. Toggles the `dark` class on <html> and persists the
// user's choice in localStorage. Kept dependency-free so it can be copied with the components.

const DARK_QUERY = '(prefers-color-scheme: dark)';

let media = null;
let listener = null;
let dotnetRef = null;
let mode = 'system';

function ensureMedia() {
    if (!media) {
        media = window.matchMedia(DARK_QUERY);
    }

    return media;
}

function normalize(value, fallback) {
    return value === 'light' || value === 'dark' || value === 'system' ? value : fallback;
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
    document.documentElement.classList.toggle('dark', resolved === 'dark');
    return resolved;
}

/**
 * Reads the persisted mode, applies the matching class and starts listening for OS scheme changes.
 * Calling it again replaces the previous subscription.
 * @param {string} storageKey localStorage key holding 'light' | 'dark' | 'system'.
 * @param {object} ref DotNetObjectReference whose OnSystemSchemeChanged(bool) receives OS changes.
 * @param {string} defaultMode 'light' | 'dark' | 'system'; used when nothing valid is stored. Defaults to 'system'.
 * @returns {{ mode: string, resolved: string }}
 */
export function init(storageKey, ref, defaultMode = 'system') {
    dispose();

    dotnetRef = ref;
    mode = readStoredMode(storageKey, normalize(defaultMode, 'system'));
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
 * @param {string} storageKey localStorage key to write.
 * @param {string} next 'light' | 'dark' | 'system'.
 * @returns {string} The resolved scheme, 'light' or 'dark'.
 */
export function setMode(storageKey, next) {
    mode = normalize(next, 'system');

    try {
        localStorage.setItem(storageKey, mode);
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
