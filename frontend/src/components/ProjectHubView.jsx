import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';
import AddKnowledgeDialog from './AddKnowledgeDialog.jsx';
import AnalyseRepoDialog from './AnalyseRepoDialog.jsx';
import MemoryChart from './MemoryChart.jsx';

const SUB_LAUNCHERS = [
    { kind: 'chat', label: 'Chat', tooltip: 'Talk to the AI in this project' },
    { kind: 'files', label: 'Files', tooltip: 'Browse the working directory' },
    { kind: 'agents', label: 'Agents', tooltip: 'Project agents' },
    { kind: 'plan', label: 'Plan', tooltip: 'Step-by-step plan for this work' },
    { kind: 'memory-tweak', label: 'Memory', tooltip: 'Tune what is sent to the AI' }
];

function formatTimestamp(iso) {
    if (!iso) return '';
    try { return new Date(iso).toLocaleString(); } catch { return iso; }
}

/**
 * Body of a "project" tab. Shows project metadata, repo + ticket pointers,
 * project / client knowledge, agents, tickets, memory usage, and the sub-tab
 * launchers. Replaces the former Info sub-tab.
 */
export default function ProjectHubView({ project, client, onProjectChanged, onOpenSub, onError }) {
    const [description, setDescription] = useState(project.description ?? '');
    const [savingDescription, setSavingDescription] = useState(false);
    const [tickets, setTickets] = useState([]);
    const [repos, setRepos] = useState([]);
    const [stepReviews, setStepReviews] = useState([]);
    const [projectKnowledge, setProjectKnowledge] = useState([]);
    const [clientKnowledge, setClientKnowledge] = useState([]);
    const [agents, setAgents] = useState([]);
    const [memory, setMemory] = useState(null);
    const [showAddProjectKnowledge, setShowAddProjectKnowledge] = useState(false);
    const [showAddClientKnowledge, setShowAddClientKnowledge] = useState(false);
    const [showAnalyseRepo, setShowAnalyseRepo] = useState(false);
    const [analysing, setAnalysing] = useState(false);

    const hasRepo = Boolean(project.workingDirectory);

    useEffect(() => setDescription(project.description ?? ''), [project.description]);

    const loadTickets = useCallback(async () => {
        try { setTickets((await api.listTickets(project.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    const loadRepos = useCallback(async () => {
        if (!client?.id) return;
        try { setRepos((await api.listClientRepos(client.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [client?.id, onError]);

    const loadStepReviews = useCallback(async () => {
        try { setStepReviews((await api.listStepReviews(project.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    const loadProjectKnowledge = useCallback(async () => {
        try { setProjectKnowledge((await api.listKnowledge(project.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    const loadClientKnowledge = useCallback(async () => {
        if (!client?.id) { setClientKnowledge([]); return; }
        try { setClientKnowledge((await api.listClientKnowledge(client.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [client?.id, onError]);

    const loadAgents = useCallback(async () => {
        try { setAgents((await api.listAgents(project.id)) ?? []); } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    const loadMemory = useCallback(async () => {
        try { setMemory(await api.memoryUsage(project.id)); } catch (err) { onError?.(err.message); }
    }, [project.id, onError]);

    useEffect(() => {
        loadTickets();
        loadRepos();
        loadStepReviews();
        loadProjectKnowledge();
        loadClientKnowledge();
        loadAgents();
        loadMemory();
    }, [loadTickets, loadRepos, loadStepReviews, loadProjectKnowledge, loadClientKnowledge, loadAgents, loadMemory]);

    const saveDescription = async () => {
        setSavingDescription(true);
        try {
            const updated = await api.updateProject(project.id, {
                description,
                ticketId: project.ticketId ?? null
            });
            onProjectChanged?.(updated);
        } catch (err) { onError?.(err.message); }
        finally { setSavingDescription(false); }
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

    const handleCreateProjectKnowledge = async (entry) => {
        try {
            await api.createKnowledge(project.id, entry);
            setShowAddProjectKnowledge(false);
            await loadProjectKnowledge();
        } catch (err) { onError?.(err.message); }
    };

    const handleDeleteProjectKnowledge = async (id) => {
        if (!window.confirm('Delete this knowledge entry?')) return;
        try { await api.deleteKnowledge(project.id, id); await loadProjectKnowledge(); }
        catch (err) { onError?.(err.message); }
    };

    const handleCreateClientKnowledge = async (entry) => {
        if (!client?.id) return;
        try {
            await api.createClientKnowledge(client.id, entry);
            setShowAddClientKnowledge(false);
            await loadClientKnowledge();
        } catch (err) { onError?.(err.message); }
    };

    const handleDeleteClientKnowledge = async (id) => {
        if (!client?.id) return;
        if (!window.confirm('Delete this client knowledge entry?')) return;
        try { await api.deleteClientKnowledge(client.id, id); await loadClientKnowledge(); }
        catch (err) { onError?.(err.message); }
    };

    const handleAnalyseRepo = async (target) => {
        setAnalysing(true);
        try {
            const result = await api.analyseRepo(project.id, target);
            setShowAnalyseRepo(false);
            if (result?.target === 'Client') {
                await loadClientKnowledge();
                onOpenSub('knowledge-client', { id: result.knowledge.id, clientId: client?.id });
            } else {
                await loadProjectKnowledge();
                onOpenSub('knowledge-project', { id: result.knowledge.id });
            }
        } catch (err) { onError?.(err.message); }
        finally { setAnalysing(false); }
    };

    return (
        <div className="project-hub">
            <header className="page-title-row project-hub-header">
                <div className="project-hub-identity">
                    <h1 className="page-title">Project — {project.name} ({client?.name ?? 'Unassigned'})</h1>
                    {hasRepo ? (
                        <code className="project-hub-cwd" title={project.workingDirectory}>{project.workingDirectory}</code>
                    ) : (
                        <p className="project-hub-no-repo">
                            This project has no repo assigned. Pick one below to enable chat,
                            files, and plan execution.
                        </p>
                    )}
                </div>
            </header>

            <section className="project-hub-launchers">
                {SUB_LAUNCHERS.map((l) => {
                    const disabled = (l.kind === 'plan' || l.kind === 'chat' || l.kind === 'files') && !hasRepo;
                    return (
                        <button
                            key={l.kind}
                            type="button"
                            className="project-hub-launcher"
                            onClick={() => onOpenSub(l.kind, {})}
                            title={disabled ? 'Add a repo to this project first' : l.tooltip}
                            disabled={disabled}
                        >
                            {l.label}
                        </button>
                    );
                })}
            </section>

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Description</h2>
                </header>
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

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Repo</h2>
                    <button
                        type="button"
                        className="btn btn-ghost btn-small"
                        onClick={() => setShowAnalyseRepo(true)}
                        disabled={analysing || !hasRepo}
                        title={hasRepo ? 'Run an AI analysis of the working directory' : 'Add a repo first'}
                    >
                        <span aria-hidden="true">🔍</span>
                        <span>{analysing ? 'Analysing…' : 'Analyse repo'}</span>
                    </button>
                </header>
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
                {repos.length === 0 && (
                    <p className="info-section-help">
                        No repos registered against this client yet. Add one from the
                        client page first.
                    </p>
                )}
            </section>

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Primary ticket</h2>
                </header>
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

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Memory capacity</h2>
                    <div className="info-section-actions">
                        <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            onClick={() => onOpenSub('memory-tweak', {})}
                            title="Choose what is sent with each prompt"
                        >
                            <span aria-hidden="true">🧠</span>
                            <span>Tweak memory</span>
                        </button>
                        <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            onClick={loadMemory}
                            title="Refresh"
                        >
                            <span aria-hidden="true">↻</span>
                            <span>Refresh</span>
                        </button>
                    </div>
                </header>
                {memory ? <MemoryChart usage={memory} /> : <p className="empty-state subtle no-pad">Calculating…</p>}
            </section>

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Agents</h2>
                    <div className="info-section-actions">
                        <span className="info-section-count">{agents.length}</span>
                        <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            onClick={() => onOpenSub('agents', {})}
                        >
                            <span>Manage</span>
                            <span aria-hidden="true">→</span>
                        </button>
                    </div>
                </header>
                {agents.length === 0 && (
                    <p className="empty-state subtle no-pad">
                        Define agent personas; each prefixes prompts with "You are a…".
                    </p>
                )}
                {agents.length > 0 && (
                    <ul className="info-list">
                        {agents.map((a) => (
                            <li key={a.id}>
                                <button
                                    type="button"
                                    className="info-list-item"
                                    onClick={() => onOpenSub('agent', { id: a.id })}
                                    title={a.title}
                                >
                                    <span className="info-list-icon" aria-hidden="true">🤖</span>
                                    <span className="info-list-name">{a.title}</span>
                                    <span className="info-list-date">{formatTimestamp(a.createdAt)}</span>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            {client && (
                <section className="info-section">
                    <header className="info-section-header">
                        <h2 className="info-section-title">Client knowledge</h2>
                        <button
                            type="button"
                            className="btn btn-primary btn-small"
                            onClick={() => setShowAddClientKnowledge(true)}
                        >
                            <span aria-hidden="true">＋</span>
                            <span>Add</span>
                        </button>
                    </header>
                    {clientKnowledge.length === 0 && (
                        <p className="empty-state subtle no-pad">No client-level knowledge yet.</p>
                    )}
                    {clientKnowledge.length > 0 && (
                        <ul className="info-list">
                            {clientKnowledge.map((k) => (
                                <li key={k.id}>
                                    <button
                                        type="button"
                                        className="info-list-item"
                                        onClick={() => onOpenSub('knowledge-client', { id: k.id, clientId: client.id })}
                                    >
                                        <span className="info-list-icon" aria-hidden="true">🏢</span>
                                        <span className="info-list-name">{k.title}</span>
                                        <span className="info-list-date">{formatTimestamp(k.createdAt)}</span>
                                    </button>
                                    <button
                                        type="button"
                                        className="info-list-delete"
                                        onClick={() => handleDeleteClientKnowledge(k.id)}
                                        aria-label={`Delete ${k.title}`}
                                        title="Delete"
                                    >×</button>
                                </li>
                            ))}
                        </ul>
                    )}
                </section>
            )}

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Tickets</h2>
                    <span className="info-section-count">{tickets.length}</span>
                </header>
                {tickets.length === 0 && (
                    <p className="empty-state subtle no-pad">No tickets yet.</p>
                )}
                {tickets.length > 0 && (
                    <ul className="info-list">
                        {tickets.map((t) => (
                            <li key={t.id}>
                                <button
                                    type="button"
                                    className="info-list-item"
                                    onClick={() => onOpenSub('ticket', { id: t.id })}
                                    title={t.title}
                                >
                                    <span className="info-list-code">{t.code}</span>
                                    <span className="info-list-name">{t.title}</span>
                                    <span className="info-list-date">{formatTimestamp(t.createdAt)}</span>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            <section className="info-section">
                <header className="info-section-header">
                    <h2 className="info-section-title">Project knowledge</h2>
                    <button
                        type="button"
                        className="btn btn-primary btn-small"
                        onClick={() => setShowAddProjectKnowledge(true)}
                    >
                        <span aria-hidden="true">＋</span>
                        <span>Add</span>
                    </button>
                </header>
                {projectKnowledge.length === 0 && (
                    <p className="empty-state subtle no-pad">
                        Add markdown notes for things you want pinned to this project –
                        domain glossaries, coding conventions, deploy instructions.
                    </p>
                )}
                {projectKnowledge.length > 0 && (
                    <ul className="info-list">
                        {projectKnowledge.map((k) => (
                            <li key={k.id}>
                                <button
                                    type="button"
                                    className="info-list-item"
                                    onClick={() => onOpenSub('knowledge-project', { id: k.id })}
                                    title={k.title}
                                >
                                    <span className="info-list-icon" aria-hidden="true">📘</span>
                                    <span className="info-list-name">{k.title}</span>
                                    <span className="info-list-date">{formatTimestamp(k.createdAt)}</span>
                                </button>
                                <button
                                    type="button"
                                    className="info-list-delete"
                                    onClick={() => handleDeleteProjectKnowledge(k.id)}
                                    aria-label={`Delete ${k.title}`}
                                    title="Delete"
                                >×</button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            {stepReviews.length > 0 && (
                <section className="info-section">
                    <header className="info-section-header">
                        <h2 className="info-section-title">Recent step reviews</h2>
                    </header>
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

            {showAddProjectKnowledge && (
                <AddKnowledgeDialog
                    onCreate={handleCreateProjectKnowledge}
                    onCancel={() => setShowAddProjectKnowledge(false)}
                />
            )}
            {showAddClientKnowledge && (
                <AddKnowledgeDialog
                    onCreate={handleCreateClientKnowledge}
                    onCancel={() => setShowAddClientKnowledge(false)}
                />
            )}
            {showAnalyseRepo && (
                <AnalyseRepoDialog
                    project={project}
                    onAnalyse={handleAnalyseRepo}
                    onCancel={() => setShowAnalyseRepo(false)}
                />
            )}
        </div>
    );
}
