import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from './api.js';
import Homepage from './components/Homepage.jsx';
import ClientView from './components/ClientView.jsx';
import ProjectHubView from './components/ProjectHubView.jsx';
import SubTabHost from './components/SubTabHost.jsx';
import NewProjectWizard from './components/NewProjectWizard.jsx';
import CreateClientDialog from './components/CreateClientDialog.jsx';
import WorkspaceTabBar from './components/WorkspaceTabBar.jsx';
import Toast from './components/Toast.jsx';
import { parseRoute, buildPath, pushUrl, replaceUrl, currentLocation } from './router.js';

const TAB_STORAGE_KEY = 'projecthub.tabs.v1';
const HOME_TAB = { id: 'home', kind: 'home', closable: false, payload: {} };

function loadStoredTabs() {
    try {
        const raw = localStorage.getItem(TAB_STORAGE_KEY);
        if (!raw) return null;
        const parsed = JSON.parse(raw);
        if (!Array.isArray(parsed?.tabs) || typeof parsed?.activeId !== 'string') return null;
        return parsed;
    } catch {
        return null;
    }
}

function persistTabs(tabs, activeId) {
    try {
        localStorage.setItem(TAB_STORAGE_KEY, JSON.stringify({ tabs, activeId }));
    } catch {
        // localStorage may be unavailable; ignore.
    }
}

function tabIdForRoute(route) {
    if (route.kind === 'home') return 'home';
    if (route.kind === 'client') return `client:${route.clientId}`;
    if (route.kind === 'project') return `project:${route.projectId}`;
    if (route.kind === 'sub') {
        const p = route.payload || {};
        const base = `sub:${route.projectId}:${route.subKind}`;
        if (route.subKind === 'file' || route.subKind === 'diff') return `${base}:${p.path}`;
        if (route.subKind === 'ticket' || route.subKind === 'agent' || route.subKind === 'knowledge-project') return `${base}:${p.id}`;
        if (route.subKind === 'knowledge-client') return `${base}:${p.id}`;
        if (route.subKind === 'diagnostics') return `${base}:${p.jobId}`;
        if (route.subKind === 'step-review') return `${base}:${p.reviewId}`;
        return base;
    }
    return 'home';
}

function tabFromRoute(route) {
    if (route.kind === 'home') return { ...HOME_TAB };
    if (route.kind === 'client') {
        return {
            id: `client:${route.clientId}`,
            kind: 'client',
            closable: true,
            payload: { clientId: route.clientId }
        };
    }
    if (route.kind === 'project') {
        return {
            id: `project:${route.projectId}`,
            kind: 'project',
            closable: true,
            payload: { projectId: route.projectId }
        };
    }
    return {
        id: tabIdForRoute(route),
        kind: 'sub',
        closable: true,
        payload: { subKind: route.subKind, projectId: route.projectId, ...(route.payload || {}) }
    };
}

const SUB_KIND_LABEL = {
    info: 'info', files: 'files', chat: 'chat', agents: 'agents',
    plan: 'plan', 'memory-tweak': 'memory tweak',
    file: 'file', diff: 'diff', ticket: 'ticket', agent: 'agent',
    'knowledge-project': 'knowledge', 'knowledge-client': 'knowledge',
    diagnostics: 'diagnostics', 'step-review': 'step review'
};

export default function App() {
    const [clients, setClients] = useState([]);
    const [projects, setProjects] = useState([]);
    const [error, setError] = useState(null);
    const [showProjectWizard, setShowProjectWizard] = useState(false);
    const [showCreateClient, setShowCreateClient] = useState(false);

    const [tabs, setTabs] = useState(() => {
        const stored = loadStoredTabs();
        const route = parseRoute(window.location.pathname, window.location.search);
        const tabFromUrl = tabFromRoute(route);
        if (stored) {
            const list = stored.tabs.some((t) => t.id === HOME_TAB.id) ? stored.tabs : [HOME_TAB, ...stored.tabs];
            if (!list.some((t) => t.id === tabFromUrl.id)) {
                list.push(tabFromUrl);
            }
            return list;
        }
        return tabFromUrl.id === HOME_TAB.id ? [HOME_TAB] : [HOME_TAB, tabFromUrl];
    });
    const [activeId, setActiveId] = useState(() => {
        const stored = loadStoredTabs();
        const route = parseRoute(window.location.pathname, window.location.search);
        const fromUrl = tabIdForRoute(route);
        return fromUrl !== 'home' ? fromUrl : (stored?.activeId ?? 'home');
    });

    useEffect(() => persistTabs(tabs, activeId), [tabs, activeId]);

    // Mirror the active tab to the URL.
    useEffect(() => {
        const tab = tabs.find((t) => t.id === activeId) ?? HOME_TAB;
        const path = buildPath(tab);
        replaceUrl(path);
    }, [activeId, tabs]);

    // Browser back/forward → activate the tab matching the route, opening it if missing.
    useEffect(() => {
        const onPop = () => {
            const loc = currentLocation();
            const route = parseRoute(loc.pathname, loc.search);
            const id = tabIdForRoute(route);
            setTabs((prev) => prev.some((t) => t.id === id) ? prev : [...prev, tabFromRoute(route)]);
            setActiveId(id);
        };
        window.addEventListener('popstate', onPop);
        return () => window.removeEventListener('popstate', onPop);
    }, []);

    const dismissError = useCallback(() => setError(null), []);

    const loadProjects = useCallback(async () => {
        try {
            const list = (await api.listProjects()) ?? [];
            setProjects(list);
            return list;
        } catch (err) {
            setError(err.message);
            return [];
        }
    }, []);

    const loadClients = useCallback(async () => {
        try {
            const list = (await api.listClients()) ?? [];
            setClients(list);
            return list;
        } catch (err) {
            setError(err.message);
            return [];
        }
    }, []);

    useEffect(() => { loadProjects(); loadClients(); }, [loadProjects, loadClients]);

    const clientById = useMemo(() => {
        const map = new Map();
        for (const c of clients) map.set(c.id, c);
        return map;
    }, [clients]);

    const projectById = useMemo(() => {
        const map = new Map();
        for (const p of projects) map.set(p.id, p);
        return map;
    }, [projects]);

    const labelFor = useCallback((tab) => {
        if (tab.kind === 'home') return 'Home';
        if (tab.kind === 'client') {
            const c = clientById.get(tab.payload.clientId);
            return `client | ${c?.name ?? tab.payload.clientId.slice(0, 6)}`;
        }
        if (tab.kind === 'project') {
            const p = projectById.get(tab.payload.projectId);
            const c = p ? clientById.get(p.clientId) : null;
            const cn = c?.name ?? '…';
            const pn = p?.name ?? tab.payload.projectId.slice(0, 6);
            return `project | ${cn} | ${pn}`;
        }
        // sub
        const p = projectById.get(tab.payload.projectId);
        const c = p ? clientById.get(p.clientId) : null;
        const cn = c?.name ?? '…';
        const pn = p?.name ?? tab.payload.projectId.slice(0, 6);
        const sub = tab.payload.subKind;
        let piece = SUB_KIND_LABEL[sub] ?? sub;
        if (sub === 'file' || sub === 'diff') {
            piece = `${SUB_KIND_LABEL[sub]} ${(tab.payload.path || '').split('/').pop()}`;
        }
        return `${cn} | ${pn} | ${piece}`;
    }, [clientById, projectById]);

    const colourFor = useCallback((tab) => {
        if (tab.kind === 'home') return null;
        let clientId = null;
        if (tab.kind === 'client') {
            clientId = tab.payload.clientId;
        } else {
            const p = projectById.get(tab.payload.projectId);
            clientId = p?.clientId ?? null;
        }
        return clientId ? (clientById.get(clientId)?.colour ?? null) : null;
    }, [clientById, projectById]);

    const decoratedTabs = useMemo(
        () => tabs.map((t) => ({ ...t, label: labelFor(t), colour: colourFor(t) })),
        [tabs, labelFor, colourFor]
    );

    const openTab = useCallback((tab) => {
        setTabs((prev) => prev.some((t) => t.id === tab.id) ? prev : [...prev, tab]);
        setActiveId(tab.id);
        pushUrl(buildPath(tab));
    }, []);

    const activate = useCallback((id) => {
        setActiveId(id);
        const t = tabs.find((x) => x.id === id);
        if (t) pushUrl(buildPath(t));
    }, [tabs]);

    const close = useCallback((id) => {
        if (id === HOME_TAB.id) return;
        setTabs((prev) => {
            const idx = prev.findIndex((t) => t.id === id);
            if (idx < 0) return prev;
            const next = prev.filter((t) => t.id !== id);
            setActiveId((cur) => {
                if (cur !== id) return cur;
                const fallback = next[Math.max(0, idx - 1)] ?? next[0] ?? HOME_TAB;
                pushUrl(buildPath(fallback));
                return fallback.id;
            });
            return next;
        });
    }, []);

    // Helpers wired into Home / Client / Project hub views.
    const openClientTab = useCallback((clientId) => {
        openTab({ id: `client:${clientId}`, kind: 'client', closable: true, payload: { clientId } });
    }, [openTab]);

    const openProjectTab = useCallback((projectId) => {
        openTab({ id: `project:${projectId}`, kind: 'project', closable: true, payload: { projectId } });
    }, [openTab]);

    const openSubTab = useCallback((projectId, subKind, payload = {}) => {
        const id = (() => {
            const base = `sub:${projectId}:${subKind}`;
            if (subKind === 'file' || subKind === 'diff') return `${base}:${payload.path}`;
            if (subKind === 'ticket' || subKind === 'agent' || subKind === 'knowledge-project' || subKind === 'knowledge-client') return `${base}:${payload.id}`;
            if (subKind === 'diagnostics') return `${base}:${payload.jobId}`;
            if (subKind === 'step-review') return `${base}:${payload.reviewId}`;
            return base;
        })();
        openTab({ id, kind: 'sub', closable: true, payload: { ...payload, subKind, projectId } });
    }, [openTab]);

    const handleProjectChanged = useCallback((updated) => {
        setProjects((prev) => prev.map((p) => p.id === updated.id ? updated : p));
    }, []);

    const handleClientChanged = useCallback((updated) => {
        setClients((prev) => prev.map((c) => c.id === updated.id ? updated : c));
    }, []);

    const handleProjectWizardComplete = async (project) => {
        setShowProjectWizard(false);
        await Promise.all([loadProjects(), loadClients()]);
        openProjectTab(project.id);
    };

    const handleCreateClient = async (name) => {
        const created = await api.createClient(name);
        setShowCreateClient(false);
        await loadClients();
        if (created?.id) openClientTab(created.id);
    };

    const activeTab = decoratedTabs.find((t) => t.id === activeId) ?? decoratedTabs[0];
    const activeProject = activeTab?.payload?.projectId
        ? projectById.get(activeTab.payload.projectId)
        : null;

    return (
        <div className="app workspace" data-mode={activeTab?.kind ?? 'home'}>
            <WorkspaceTabBar
                tabs={decoratedTabs}
                activeId={activeId}
                onActivate={activate}
                onClose={close}
            />

            <div className="workspace-body">
                {activeTab?.kind === 'home' && (
                    <Homepage
                        clients={clients}
                        projects={projects}
                        onPickClient={openClientTab}
                        onPickProject={openProjectTab}
                        onNewClient={() => setShowCreateClient(true)}
                        onNewProject={() => setShowProjectWizard(true)}
                    />
                )}

                {activeTab?.kind === 'client' && (
                    <ClientView
                        clientId={activeTab.payload.clientId}
                        client={clientById.get(activeTab.payload.clientId)}
                        projects={projects.filter((p) => p.clientId === activeTab.payload.clientId)}
                        onClientChanged={handleClientChanged}
                        onProjectsChanged={loadProjects}
                        onOpenProject={openProjectTab}
                        onNewProject={() => setShowProjectWizard(true)}
                        onError={setError}
                    />
                )}

                {activeTab?.kind === 'project' && activeProject && (
                    <ProjectHubView
                        project={activeProject}
                        client={clientById.get(activeProject.clientId)}
                        onProjectChanged={handleProjectChanged}
                        onOpenSub={(subKind, payload) => openSubTab(activeProject.id, subKind, payload)}
                        onError={setError}
                    />
                )}

                {activeTab?.kind === 'project' && !activeProject && (
                    <div className="workspace-empty">Loading project…</div>
                )}

                {activeTab?.kind === 'sub' && activeProject && (
                    <SubTabHost
                        project={activeProject}
                        client={clientById.get(activeProject.clientId)}
                        subKind={activeTab.payload.subKind}
                        payload={activeTab.payload}
                        onProjectChanged={handleProjectChanged}
                        onOpenSub={(subKind, payload) => openSubTab(activeProject.id, subKind, payload)}
                        onError={setError}
                    />
                )}

                {activeTab?.kind === 'sub' && !activeProject && (
                    <div className="workspace-empty">Loading project…</div>
                )}
            </div>

            {showProjectWizard && (
                <NewProjectWizard
                    clients={clients}
                    onComplete={handleProjectWizardComplete}
                    onCancel={() => setShowProjectWizard(false)}
                    onClientsChanged={loadClients}
                />
            )}

            {showCreateClient && (
                <CreateClientDialog
                    onCreate={handleCreateClient}
                    onCancel={() => setShowCreateClient(false)}
                />
            )}

            <Toast message={error} onDismiss={dismissError} />
        </div>
    );
}
