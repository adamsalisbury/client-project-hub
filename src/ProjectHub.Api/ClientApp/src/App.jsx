import { useCallback, useEffect, useState } from 'react';
import { api } from './api.js';
import Homepage from './components/Homepage.jsx';
import NewProjectWizard from './components/NewProjectWizard.jsx';
import CreateClientDialog from './components/CreateClientDialog.jsx';
import ProjectView from './components/ProjectView.jsx';
import Toast from './components/Toast.jsx';
import { parseRoute, replaceUrl, currentLocation } from './router.js';

export default function App() {
    const [projects, setProjects] = useState([]);
    const [clients, setClients] = useState([]);
    const [route, setRoute] = useState(() => parseRoute(window.location.pathname, window.location.search));
    const [showProjectWizard, setShowProjectWizard] = useState(false);
    const [showCreateClient, setShowCreateClient] = useState(false);
    const [error, setError] = useState(null);

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

    useEffect(() => {
        loadProjects();
        loadClients();
    }, [loadProjects, loadClients]);

    useEffect(() => {
        const onPop = () => {
            const loc = currentLocation();
            setRoute(parseRoute(loc.pathname, loc.search));
        };
        window.addEventListener('popstate', onPop);
        return () => window.removeEventListener('popstate', onPop);
    }, []);

    const goToProject = useCallback((projectId, target = { kind: 'info' }) => {
        if (!projectId) {
            replaceUrl('/');
            setRoute({ projectId: null, target: null });
            return;
        }
        setRoute({ projectId, target });
        replaceUrl(`/project/${projectId}/${target.kind}`);
    }, []);

    const goHome = useCallback(() => {
        replaceUrl('/');
        setRoute({ projectId: null, target: null });
    }, []);

    const handleProjectWizardComplete = async (project) => {
        setShowProjectWizard(false);
        await Promise.all([loadProjects(), loadClients()]);
        goToProject(project.id);
    };

    const handleCreateClient = async (name) => {
        await api.createClient(name);
        setShowCreateClient(false);
        await loadClients();
    };

    const currentProject = route.projectId
        ? projects.find((p) => p.id === route.projectId) ?? null
        : null;

    return (
        <div className="app" data-mode={currentProject ? 'project' : 'home'}>
            {!currentProject && (
                <Homepage
                    clients={clients}
                    projects={projects}
                    onPick={(id) => goToProject(id)}
                    onNewClient={() => setShowCreateClient(true)}
                    onNewProject={() => setShowProjectWizard(true)}
                />
            )}

            {currentProject && (
                <ProjectView
                    project={currentProject}
                    initialTarget={route.target ?? { kind: 'info' }}
                    onSwitchProject={goHome}
                    onProjectChanged={(updated) => {
                        setProjects((prev) => prev.map((p) => p.id === updated.id ? updated : p));
                    }}
                    onError={setError}
                />
            )}

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
