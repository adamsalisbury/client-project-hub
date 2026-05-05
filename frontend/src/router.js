/**
 * Translates the current URL into project + tab state and back.
 *
 * Routes:
 *   /                              homepage
 *   /project/:id                   project view, info tab
 *   /project/:id/info              info
 *   /project/:id/files             files
 *   /project/:id/agents            agents
 *   /project/:id/chat              chat
 *   /project/:id/memory-tweak      memory tweaking
 *   /project/:id/file/<path>       file detail (path may contain slashes)
 *   /project/:id/diff/<path>       diff
 *   /project/:id/ticket/<tid>      ticket
 *   /project/:id/agent/<aid>       single agent
 *   /project/:id/knowledge/<kid>           project knowledge
 *   /project/:id/client-knowledge/<kid>    client knowledge (with clientId in query)
 *   /project/:id/diagnostics/<jobId>       diagnostics for a specific message
 */

export const PERMANENT_KINDS = new Set(['info', 'files', 'agents', 'chat', 'memory-tweak']);

export function parseRoute(pathname, search) {
    if (!pathname || pathname === '/') {
        return { projectId: null, target: null };
    }

    const m = pathname.match(/^\/project\/([^/]+)(?:\/(.*?))?\/?$/);
    if (!m) {
        return { projectId: null, target: null };
    }

    const projectId = m[1];
    const rest = (m[2] || '').replace(/^\/+|\/+$/g, '');
    if (!rest) {
        return { projectId, target: { kind: 'info' } };
    }

    const segs = rest.split('/');
    const head = segs[0];
    const tail = segs.slice(1).join('/');

    if (PERMANENT_KINDS.has(head)) {
        return { projectId, target: { kind: head } };
    }
    if (head === 'file' && tail) {
        return { projectId, target: { kind: 'file', path: decodeURIComponent(tail) } };
    }
    if (head === 'diff' && tail) {
        return { projectId, target: { kind: 'diff', path: decodeURIComponent(tail) } };
    }
    if (head === 'ticket' && tail) {
        return { projectId, target: { kind: 'ticket', id: tail } };
    }
    if (head === 'agent' && tail) {
        return { projectId, target: { kind: 'agent', id: tail } };
    }
    if (head === 'knowledge' && tail) {
        return { projectId, target: { kind: 'knowledge-project', id: tail } };
    }
    if (head === 'diagnostics' && tail) {
        return { projectId, target: { kind: 'diagnostics', id: tail } };
    }
    if (head === 'client-knowledge' && tail) {
        const params = new URLSearchParams(search || '');
        return {
            projectId,
            target: {
                kind: 'knowledge-client',
                id: tail,
                clientId: params.get('client') || null
            }
        };
    }

    return { projectId, target: { kind: 'info' } };
}

export function buildPath(projectId, tab) {
    if (!projectId) return '/';
    const base = `/project/${projectId}`;
    if (!tab) return `${base}/info`;
    switch (tab.kind) {
        case 'info':
        case 'files':
        case 'agents':
        case 'chat':
        case 'memory-tweak':
            return `${base}/${tab.kind}`;
        case 'file':
            return `${base}/file/${encodePath(tab.payload?.path)}`;
        case 'diff':
            return `${base}/diff/${encodePath(tab.payload?.path)}`;
        case 'ticket':
            return `${base}/ticket/${tab.payload?.ticketId}`;
        case 'agent':
            return `${base}/agent/${tab.payload?.agentId}`;
        case 'diagnostics':
            return `${base}/diagnostics/${tab.payload?.jobId}`;
        case 'knowledge-project':
            return `${base}/knowledge/${tab.payload?.id}`;
        case 'knowledge-client':
            return `${base}/client-knowledge/${tab.payload?.id}?client=${encodeURIComponent(tab.payload?.clientId ?? '')}`;
        default:
            return `${base}/info`;
    }
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
