import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';

const SUB_LAUNCHERS = [
    { kind: 'info', label: 'Info', tooltip: 'Project metadata and tickets' },
    { kind: 'chat', label: 'Chat', tooltip: 'Talk to Claude in this project' },
    { kind: 'files', label: 'Files', tooltip: 'Browse the working directory' },
    { kind: 'agents', label: 'Agents', tooltip: 'Project agents' },
    { kind: 'plan', label: 'Plan', tooltip: 'Step-by-step plan for this work' },
    { kind: 'memory-tweak', label: 'Memory', tooltip: 'Tune what is sent to Claude' }
];

/**
 * Body of a "project" tab. Shows project metadata + a description editor +
 * a primary-ticket pointer + a repo pointer, and a row of launchers that
 * spawn sub-tabs at the workspace level.
 */
export default function ProjectHubView({ project, client, onProjectChanged, onOpenSub, onError }) {
    const [description, setDescription] = useState(project.description ?? '');
    const [savingDescription, setSavingDescription] = useState(false);
    const [tickets, setTickets] = useState([]);
    const [repos, setRepos] = useState([]);
    const [stepReviews, setStepReviews] = useState([]);

    useEffect(() => setDescription(project.description ?? ''), [project.description]);

    const loadTickets = useCallback(async () => {
        try {
            setTickets((await api.listTickets(project.id)) ?? []);
        } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    const loadRepos = useCallback(async () => {
        if (!client?.id) return;
        try {
            setRepos((await api.listClientRepos(client.id)) ?? []);
        } catch (err) { onError?.(err.message); }
    }, [client?.id, onError]);

    const loadStepReviews = useCallback(async () => {
        try {
            setStepReviews((await api.listStepReviews(project.id)) ?? []);
        } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    useEffect(() => { loadTickets(); loadRepos(); loadStepReviews(); }, [loadTickets, loadRepos, loadStepReviews]);

    const saveDescription = async () => {
        setSavingDescription(true);
        try {
            const updated = await api.updateProject(project.id, {
                description,
                ticketId: project.ticketId ?? null
            });
            onProjectChanged?.(updated);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSavingDescription(false);
        }
    };

    const handleAssignRepo = async (repoId) => {
        try {
            const updated = await api.assignProjectRepo(project.id, repoId || null);
            onProjectChanged?.(updated);
        } catch (err) { onError?.(err.message); }
    };

    const handleAssignTicket = async (ticketId) => {
        try {
            const updated = await api.updateProject(project.id, {
                description: project.description ?? null,
                ticketId: ticketId || null
            });
            onProjectChanged?.(updated);
        } catch (err) { onError?.(err.message); }
    };

    return (
        <div className="project-hub">
            <header className="project-hub-header">
                <div className="project-hub-identity">
                    <div className="project-hub-eyebrow">{client?.name ?? 'Unassigned'}</div>
                    <h1 className="project-hub-name">{project.name}</h1>
                    <code className="project-hub-cwd" title={project.workingDirectory}>{project.workingDirectory}</code>
                </div>
            </header>

            <section className="project-hub-launchers">
                {SUB_LAUNCHERS.map((l) => (
                    <button
                        key={l.kind}
                        type="button"
                        className="project-hub-launcher"
                        onClick={() => onOpenSub(l.kind, {})}
                        title={l.tooltip}
                    >
                        {l.label}
                    </button>
                ))}
            </section>

            <section className="project-hub-meta">
                <header className="client-view-section-head"><h2>Description</h2></header>
                <textarea
                    className="text-input"
                    rows={5}
                    placeholder="What is this project for?"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    disabled={savingDescription}
                />
                <div className="modal-actions">
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={saveDescription}
                        disabled={savingDescription || description === (project.description ?? '')}
                    >
                        {savingDescription ? 'Saving…' : 'Save description'}
                    </button>
                </div>
            </section>

            <section className="project-hub-meta">
                <header className="client-view-section-head"><h2>Repo</h2></header>
                <select
                    className="project-select"
                    value={project.repoId ?? ''}
                    onChange={(e) => handleAssignRepo(e.target.value || null)}
                >
                    <option value="">— Detached —</option>
                    {repos.map((r) => (
                        <option key={r.id} value={r.id}>{r.name} — {r.path}</option>
                    ))}
                </select>
            </section>

            <section className="project-hub-meta">
                <header className="client-view-section-head"><h2>Primary ticket</h2></header>
                <select
                    className="project-select"
                    value={project.ticketId ?? ''}
                    onChange={(e) => handleAssignTicket(e.target.value || null)}
                >
                    <option value="">— None —</option>
                    {tickets.map((t) => (
                        <option key={t.id} value={t.id}>{t.code} — {t.title}</option>
                    ))}
                </select>
            </section>

            {stepReviews.length > 0 && (
                <section className="project-hub-meta">
                    <header className="client-view-section-head"><h2>Recent step reviews</h2></header>
                    <ul className="step-review-list">
                        {stepReviews.slice(0, 5).map((r) => (
                            <li key={r.id}>
                                <button
                                    type="button"
                                    className="client-project-item"
                                    onClick={() => onOpenSub('step-review', { reviewId: r.id })}
                                >
                                    <span className="client-project-name">
                                        {new Date(r.createdAt).toLocaleString()} — {r.files.length} file(s)
                                    </span>
                                    <span className="client-project-cwd">
                                        {r.files.filter((f) => f.state === 'Pending').length} pending
                                    </span>
                                </button>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
        </div>
    );
}
