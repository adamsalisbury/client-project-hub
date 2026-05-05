import { useMemo } from 'react';

export default function Homepage({ clients, projects, onPick, onNewClient, onNewProject }) {
    const projectsByClient = useMemo(() => {
        const map = new Map();
        for (const project of projects) {
            const list = map.get(project.clientId) ?? [];
            list.push(project);
            map.set(project.clientId, list);
        }
        return map;
    }, [projects]);

    const orderedClients = useMemo(
        () => [...clients].sort((a, b) => (a.name || '').localeCompare(b.name || '')),
        [clients]
    );

    return (
        <div className="homepage">
            <div className="homepage-card">
                <h1 className="homepage-title">Project Hub</h1>
                <p className="homepage-tagline">
                    Pick a project, or start something new.
                </p>

                <div className="homepage-actions">
                    <button
                        type="button"
                        className="btn btn-ghost homepage-cta"
                        onClick={onNewClient}
                    >
                        <span aria-hidden="true">＋</span>
                        <span>New client</span>
                    </button>
                    <button
                        type="button"
                        className="btn btn-primary homepage-cta"
                        onClick={onNewProject}
                        disabled={clients.length === 0}
                        title={clients.length === 0 ? 'Create a client first' : undefined}
                    >
                        <span aria-hidden="true">＋</span>
                        <span>New project</span>
                    </button>
                </div>

                {clients.length === 0 && (
                    <p className="empty-state subtle">
                        No clients yet. Create one to start adding projects.
                    </p>
                )}

                {orderedClients.length > 0 && (
                    <ul className="client-list">
                        {orderedClients.map((c) => {
                            const list = projectsByClient.get(c.id) ?? [];
                            return (
                                <li key={c.id} className="client-list-item">
                                    <div className="client-list-header">
                                        <span className="client-list-name">{c.name}</span>
                                        <span className="client-list-count">
                                            {list.length} {list.length === 1 ? 'project' : 'projects'}
                                        </span>
                                    </div>
                                    {list.length === 0 && (
                                        <p className="empty-state subtle no-pad">No projects yet.</p>
                                    )}
                                    {list.length > 0 && (
                                        <ul className="client-project-list">
                                            {list.map((p) => (
                                                <li key={p.id}>
                                                    <button
                                                        type="button"
                                                        className="client-project-item"
                                                        onClick={() => onPick(p.id)}
                                                        title={p.workingDirectory}
                                                    >
                                                        <span className="client-project-name">{p.name}</span>
                                                        <code className="client-project-cwd">{p.workingDirectory}</code>
                                                    </button>
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </li>
                            );
                        })}
                    </ul>
                )}
            </div>
        </div>
    );
}
