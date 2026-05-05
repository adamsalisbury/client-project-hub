import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';
import AddAgentDialog from './AddAgentDialog.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

export default function AgentsTab({ project, openAgent, onError }) {
    const [agents, setAgents] = useState([]);
    const [loading, setLoading] = useState(false);
    const [showAdd, setShowAdd] = useState(false);

    const reload = useCallback(async () => {
        setLoading(true);
        try {
            const list = (await api.listAgents(project.id)) ?? [];
            setAgents(list);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoading(false);
        }
    }, [project.id, onError]);

    useEffect(() => { reload(); }, [reload]);

    const handleCreate = async (agent) => {
        const created = await api.createAgent(project.id, agent);
        setShowAdd(false);
        await reload();
        openAgent?.(created);
    };

    const handleDelete = async (id, e) => {
        e.stopPropagation();
        if (!confirm('Delete this agent?')) return;
        try {
            await api.deleteAgent(project.id, id);
            await reload();
        } catch (err) {
            onError?.(err.message);
        }
    };

    return (
        <div className="agents-tab">
            <header className="agents-tab-header">
                <div className="agents-tab-title">
                    <h1 className="document-tab-title">Agents</h1>
                    <p className="modal-help">
                        Each agent prefixes the prompt with “You are a …, your characteristics are …”.
                        Mark which agents are active in the Memory Tweaking tab.
                    </p>
                </div>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => setShowAdd(true)}
                >
                    <span aria-hidden="true">＋</span>
                    <span>New agent</span>
                </button>
            </header>

            <div className="agents-tab-body">
                {loading && agents.length === 0 && (
                    <p className="empty-state subtle">Loading…</p>
                )}
                {!loading && agents.length === 0 && (
                    <p className="empty-state subtle">
                        No agents yet. Add one to give the AI a persona on each prompt.
                    </p>
                )}
                {agents.length > 0 && (
                    <ul className="agents-grid">
                        {agents.map((a) => (
                            <li key={a.id}>
                                <button
                                    type="button"
                                    className="agent-card"
                                    onClick={() => openAgent?.(a)}
                                    title={a.title}
                                >
                                    <div className="agent-card-row1">
                                        <span className="agent-card-icon" aria-hidden="true">🤖</span>
                                        <span className="agent-card-title">{a.title}</span>
                                    </div>
                                    <span className="agent-card-date">added {formatTimestamp(a.createdAt)}</span>
                                </button>
                                <button
                                    type="button"
                                    className="info-list-delete"
                                    onClick={(e) => handleDelete(a.id, e)}
                                    aria-label={`Delete ${a.title}`}
                                    title="Delete"
                                >×</button>
                            </li>
                        ))}
                    </ul>
                )}
            </div>

            {showAdd && (
                <AddAgentDialog
                    projectId={project.id}
                    onCreate={handleCreate}
                    onCancel={() => setShowAdd(false)}
                />
            )}
        </div>
    );
}
