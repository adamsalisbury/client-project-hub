/**
 * URL ↔ active workspace tab.
 *
 * The workspace shell keeps its full open-tab list in localStorage; only the
 * *active* tab is reflected in the URL so browser back/forward navigates
 * between tabs.
 *
 * Routes:
 *   /                                   home
 *   /c/<clientId>                       client tab
 *   /p/<projectId>                      project hub tab
 *   /p/<projectId>/<subKind>            permanent sub (info|files|chat|agents|plan|memory-tweak)
 *   /p/<projectId>/file/<path>          file viewer sub
 *   /p/<projectId>/diff/<path>          diff sub
 *   /p/<projectId>/ticket/<tid>         ticket sub
 *   /p/<projectId>/agent/<aid>          agent sub
 *   /p/<projectId>/knowledge/<kid>      project-knowledge sub
 *   /p/<projectId>/client-knowledge/<kid>?client=<cid>   client-knowledge sub
 *   /p/<projectId>/diagnostics/<jobId>  diagnostics sub
 *   /p/<projectId>/step-review/<rid>    step-review sub
 *
 * Legacy `/project/<id>/...` URLs still parse (mapped to the same shapes).
 */

const PERMANENT_SUB_KINDS = new Set(['files', 'chat', 'agents', 'plan', 'memory-tweak']);

export function parseRoute(pathname, search) {
    const params = new URLSearchParams(search || '');

    if (!pathname || pathname === '/') {
        return { kind: 'home' };
    }

    if (pathname === '/settings' || pathname === '/settings/') {
        return { kind: 'settings' };
    }

    let m = pathname.match(/^\/c\/([^/]+)\/?$/);
    if (m) {
        return { kind: 'client', clientId: m[1] };
    }

    m = pathname.match(/^\/(?:p|project)\/([^/]+)(?:\/(.*?))?\/?$/);
    if (!m) {
        return { kind: 'home' };
    }

    const projectId = m[1];
    const rest = (m[2] || '').replace(/^\/+|\/+$/g, '');
    if (!rest) {
        return { kind: 'project', projectId };
    }

    const segs = rest.split('/');
    const head = segs[0];
    const tail = segs.slice(1).join('/');

    if (PERMANENT_SUB_KINDS.has(head)) {
        return { kind: 'sub', subKind: head, projectId };
    }
    if (head === 'file' && tail) {
        return { kind: 'sub', subKind: 'file', projectId, payload: { path: decodeURIComponent(tail) } };
    }
    if (head === 'diff' && tail) {
        return { kind: 'sub', subKind: 'diff', projectId, payload: { path: decodeURIComponent(tail) } };
    }
    if (head === 'ticket' && tail) {
        return { kind: 'sub', subKind: 'ticket', projectId, payload: { id: tail } };
    }
    if (head === 'agent' && tail) {
        return { kind: 'sub', subKind: 'agent', projectId, payload: { id: tail } };
    }
    if (head === 'knowledge' && tail) {
        return { kind: 'sub', subKind: 'knowledge-project', projectId, payload: { id: tail } };
    }
    if (head === 'client-knowledge' && tail) {
        return { kind: 'sub', subKind: 'knowledge-client', projectId, payload: { id: tail, clientId: params.get('client') || null } };
    }
    if (head === 'diagnostics' && tail) {
        return { kind: 'sub', subKind: 'diagnostics', projectId, payload: { jobId: tail } };
    }
    if (head === 'step-review' && tail) {
        return { kind: 'sub', subKind: 'step-review', projectId, payload: { reviewId: tail } };
    }

    return { kind: 'project', projectId };
}

export function buildPath(tab) {
    if (!tab || tab.kind === 'home') return '/';
    if (tab.kind === 'settings') return '/settings';
    if (tab.kind === 'client') return `/c/${tab.payload.clientId}`;
    if (tab.kind === 'project') return `/p/${tab.payload.projectId}`;
    if (tab.kind === 'sub') {
        const base = `/p/${tab.payload.projectId}`;
        const sub = tab.payload.subKind;
        switch (sub) {
            case 'files':
            case 'chat':
            case 'agents':
            case 'plan':
            case 'memory-tweak':
                return `${base}/${sub}`;
            case 'file':
                return `${base}/file/${encodePath(tab.payload.path)}`;
            case 'diff':
                return `${base}/diff/${encodePath(tab.payload.path)}`;
            case 'ticket':
                return `${base}/ticket/${tab.payload.id}`;
            case 'agent':
                return `${base}/agent/${tab.payload.id}`;
            case 'diagnostics':
                return `${base}/diagnostics/${tab.payload.jobId}`;
            case 'knowledge-project':
                return `${base}/knowledge/${tab.payload.id}`;
            case 'knowledge-client':
                return `${base}/client-knowledge/${tab.payload.id}?client=${encodeURIComponent(tab.payload.clientId ?? '')}`;
            case 'step-review':
                return `${base}/step-review/${tab.payload.reviewId}`;
            default:
                return base;
        }
    }
    return '/';
}

function encodePath(p) {
    if (!p) return '';
    return p.split('/').map(encodeURIComponent).join('/');
}

export function pushUrl(path) {
    if (window.location.pathname + window.location.search === path) return;
    window.history.pushState(null, '', path);
}

export function replaceUrl(path) {
    if (window.location.pathname + window.location.search === path) return;
    window.history.replaceState(null, '', path);
}

export function currentLocation() {
    return { pathname: window.location.pathname, search: window.location.search };
}
