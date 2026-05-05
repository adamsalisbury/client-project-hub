import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';
import AddKnowledgeDialog from './AddKnowledgeDialog.jsx';
import CreateClientDialog from './CreateClientDialog.jsx';
import AnalyseRepoDialog from './AnalyseRepoDialog.jsx';
import MemoryChart from './MemoryChart.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

export default function InfoTab({
    project,
    tickets,
    openTicket,
    openProjectKnowledge,
    openClientKnowledge,
    openAgents,
    openAgent,
    openMemoryTweak,
    openProjectKnowledgeById,
    openClientKnowledgeById,
    onProjectChanged,
    onError
}) {
    const [knowledge, setKnowledge] = useState([]);
    const [loadingKnowledge, setLoadingKnowledge] = useState(false);
    const [showAddProjectKnowledge, setShowAddProjectKnowledge] = useState(false);

    const [clients, setClients] = useState([]);
    const [clientKnowledge, setClientKnowledge] = useState([]);
    const [loadingClientKnowledge, setLoadingClientKnowledge] = useState(false);
    const [showCreateClient, setShowCreateClient] = useState(false);
    const [showAddClientKnowledge, setShowAddClientKnowledge] = useState(false);
    const [assigning, setAssigning] = useState(false);

    const [memory, setMemory] = useState(null);
    const [loadingMemory, setLoadingMemory] = useState(false);

    const [agents, setAgents] = useState([]);
    const [loadingAgents, setLoadingAgents] = useState(false);

    const [showAnalyseRepo, setShowAnalyseRepo] = useState(false);
    const [analysing, setAnalysing] = useState(false);

    const reloadKnowledge = useCallback(async () => {
        if (!project) return;
        setLoadingKnowledge(true);
        try {
            const list = (await api.listKnowledge(project.id)) ?? [];
            setKnowledge(list);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoadingKnowledge(false);
        }
    }, [project?.id, onError]);

    const reloadClients = useCallback(async () => {
        try {
            const list = (await api.listClients()) ?? [];
            setClients(list);
        } catch (err) {
            onError?.(err.message);
        }
    }, [onError]);

    const reloadClientKnowledge = useCallback(async () => {
        if (!project?.clientId) {
            setClientKnowledge([]);
            return;
        }
        setLoadingClientKnowledge(true);
        try {
            const list = (await api.listClientKnowledge(project.clientId)) ?? [];
            setClientKnowledge(list);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoadingClientKnowledge(false);
        }
    }, [project?.clientId, onError]);

    const reloadMemory = useCallback(async () => {
        if (!project) return;
        setLoadingMemory(true);
        try {
            const data = await api.memoryUsage(project.id);
            setMemory(data);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoadingMemory(false);
        }
    }, [project?.id, onError]);

    const reloadAgents = useCallback(async () => {
        if (!project) return;
        setLoadingAgents(true);
        try {
            const list = (await api.listAgents(project.id)) ?? [];
            setAgents(list);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoadingAgents(false);
        }
    }, [project?.id, onError]);

    useEffect(() => { reloadKnowledge(); }, [reloadKnowledge]);
    useEffect(() => { reloadClients(); }, [reloadClients]);
    useEffect(() => { reloadClientKnowledge(); }, [reloadClientKnowledge]);
    useEffect(() => { reloadAgents(); }, [reloadAgents]);
    useEffect(() => { reloadMemory(); }, [reloadMemory, knowledge.length, clientKnowledge.length, tickets.length, agents.length]);

    const handleCreateProjectKnowledge = async (entry) => {
        await api.createKnowledge(project.id, entry);
        setShowAddProjectKnowledge(false);
        await reloadKnowledge();
    };

    const handleDeleteProjectKnowledge = async (id) => {
        if (!confirm('Delete this knowledge entry?')) return;
        try {
            await api.deleteKnowledge(project.id, id);
            await reloadKnowledge();
        } catch (err) {
            onError?.(err.message);
        }
    };

    const handleAssignClient = async (clientId) => {
        if (!clientId) return;
        setAssigning(true);
        try {
            const updated = await api.assignClient(project.id, clientId);
            onProjectChanged?.(updated);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setAssigning(false);
        }
    };

    const handleCreateClient = async (name) => {
        const created = await api.createClient(name);
        setShowCreateClient(false);
        await reloadClients();
        await handleAssignClient(created.id);
    };

    const handleCreateClientKnowledge = async (entry) => {
        await api.createClientKnowledge(project.clientId, entry);
        setShowAddClientKnowledge(false);
        await reloadClientKnowledge();
    };

    const handleDeleteClientKnowledge = async (id) => {
        if (!confirm('Delete this client knowledge entry?')) return;
        try {
            await api.deleteClientKnowledge(project.clientId, id);
            await reloadClientKnowledge();
        } catch (err) {
            onError?.(err.message);
        }
    };

    const currentClient = clients.find((c) => c.id === project.clientId);

    const handleAnalyseRepo = async (target) => {
        setAnalysing(true);
        try {
            const result = await api.analyseRepo(project.id, target);
            setShowAnalyseRepo(false);
            if (result?.target === 'Client' && openClientKnowledgeById) {
                await reloadClientKnowledge();
                openClientKnowledgeById(result.knowledge);
            } else if (openProjectKnowledgeById) {
                await reloadKnowledge();
                openProjectKnowledgeById(result.knowledge);
            }
        } finally {
            setAnalysing(false);
        }
    };

    return (
        <div className="info-tab">
            <section className="info-section">
                <div className="info-section-header">
                    <h2 className="info-section-title">Project</h2>
                    <button
                        type="button"
                        className="btn btn-ghost btn-small"
                        onClick={() => setShowAnalyseRepo(true)}
                        disabled={analysing}
                        title="Run a Claude Code analysis of the working directory"
                    >
                        <span aria-hidden="true">🔍</span>
                        <span>{analysing ? 'Analysing.' : 'Analyse repo'}</span>
                    </button>
                </div>
                <dl className="info-grid">
                    <dt>Name</dt>
                    <dd>{project.name}</dd>
                    <dt>Working directory</dt>
                    <dd><code className="info-mono">{project.workingDirectory}</code></dd>
                    <dt>Created</dt>
                    <dd>{formatTimestamp(project.createdAt)}</dd>
                </dl>
            </section>

            <section className="info-section">
                <div className="info-section-header">
                    <h2 className="info-section-title">Memory capacity</h2>
                    <div className="info-section-actions">
                        {openMemoryTweak && (
                            <button
                                type="button"
                                className="btn btn-ghost btn-small"
                                onClick={openMemoryTweak}
                                title="Choose what is sent with each prompt"
                            >
                                <span aria-hidden="true">🧠</span>
                                <span>Tweak memory</span>
                            </button>
                        )}
                        {!loadingMemory && memory && (
                            <button
                                type="button"
                                className="btn btn-ghost btn-small"
                                onClick={reloadMemory}
                                title="Refresh"
                            >
                                <span aria-hidden="true">↻</span>
                                <span>Refresh</span>
                            </button>
                        )}
                    </div>
                </div>
                {loadingMemory && !memory ? (
                    <p className="empty-state subtle no-pad">Calculating…</p>
                ) : (
                    <MemoryChart usage={memory} />
                )}
            </section>

            <section className="info-section">
                <div className="info-section-header">
                    <h2 className="info-section-title">Agents</h2>
                    <div className="info-section-actions">
                        <span className="info-section-count">{agents.length}</span>
                        <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            onClick={openAgents}
                        >
                            <span>Manage</span>
                            <span aria-hidden="true">→</span>
                        </button>
                    </div>
                </div>
                {loadingAgents && agents.length === 0 && (
                    <p className="empty-state subtle no-pad">Loading…</p>
                )}
                {!loadingAgents && agents.length === 0 && (
                    <p className="empty-state subtle no-pad">
                        Define agent personas; each prefixes prompts with “You are a …”.
                    </p>
                )}
                {agents.length > 0 && (
                    <ul className="info-list">
                        {agents.map((a) => (
                            <li key={a.id}>
                                <button
                                    type="button"
                                    className="info-list-item"
                                    onClick={() => openAgent?.(a)}
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

            <section className="info-section">
                <div className="info-section-header">
                    <h2 className="info-section-title">Client</h2>
                </div>
                <div className="client-row">
                    <select
                        className="project-select"
                        value={project.clientId ?? ''}
                        onChange={(e) => handleAssignClient(e.target.value)}
                        disabled={assigning}
                    >
                        {clients.map((c) => (
                            <option key={c.id} value={c.id}>{c.name}</option>
                        ))}
                    </select>
                    <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setShowCreateClient(true)}
                        disabled={assigning}
                    >
                        <span aria-hidden="true">＋</span>
                        <span>New client</span>
                    </button>
                </div>
                {currentClient && (
                    <p className="info-section-help">
                        This project belongs to <strong>{currentClient.name}</strong>.
                        Its knowledge is included in every Claude prompt for this project.
                    </p>
                )}
            </section>

            {project.clientId && (
                <section className="info-section">
                    <div className="info-section-header">
                        <h2 className="info-section-title">Client knowledge</h2>
                        <button
                            type="button"
                            className="btn btn-primary btn-small"
                            onClick={() => setShowAddClientKnowledge(true)}
                        >
                            <span aria-hidden="true">＋</span>
                            <span>Add</span>
                        </button>
                    </div>
                    {loadingClientKnowledge && clientKnowledge.length === 0 && (
                        <p className="empty-state subtle no-pad">Loading…</p>
                    )}
                    {!loadingClientKnowledge && clientKnowledge.length === 0 && (
                        <p className="empty-state subtle no-pad">
                            No client-level knowledge yet.
                        </p>
                    )}
                    {clientKnowledge.length > 0 && (
                        <ul className="info-list">
                            {clientKnowledge.map((k) => (
                                <li key={k.id}>
                                    <button
                                        type="button"
                                        className="info-list-item"
                                        onClick={() => openClientKnowledge(k)}
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
                <div className="info-section-header">
                    <h2 className="info-section-title">Tickets</h2>
                    <span className="info-section-count">{tickets.length}</span>
                </div>
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
                                    onClick={() => openTicket(t)}
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
                <div className="info-section-header">
                    <h2 className="info-section-title">Project knowledge</h2>
                    <button
                        type="button"
                        className="btn btn-primary btn-small"
                        onClick={() => setShowAddProjectKnowledge(true)}
                    >
                        <span aria-hidden="true">＋</span>
                        <span>Add</span>
                    </button>
                </div>
                {loadingKnowledge && knowledge.length === 0 && (
                    <p className="empty-state subtle no-pad">Loading…</p>
                )}
                {!loadingKnowledge && knowledge.length === 0 && (
                    <p className="empty-state subtle no-pad">
                        Add markdown notes for things you want pinned to this project -
                        domain glossaries, coding conventions, deploy instructions.
                    </p>
                )}
                {knowledge.length > 0 && (
                    <ul className="info-list">
                        {knowledge.map((k) => (
                            <li key={k.id}>
                                <button
                                    type="button"
                                    className="info-list-item"
                                    onClick={() => openProjectKnowledge(k)}
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
            {showCreateClient && (
                <CreateClientDialog
                    onCreate={handleCreateClient}
                    onCancel={() => setShowCreateClient(false)}
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
