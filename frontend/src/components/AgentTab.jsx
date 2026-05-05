import { useEffect, useState } from 'react';
import { api } from '../api.js';
import MarkdownView from './MarkdownView.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

export default function AgentTab({ project, agentId, onError }) {
    const [agent, setAgent] = useState(null);
    const [loading, setLoading] = useState(false);
    const [editing, setEditing] = useState(false);
    const [editTitle, setEditTitle] = useState('');
    const [editBody, setEditBody] = useState('');
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);

    const reload = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await api.getAgent(project.id, agentId);
            if (!data) {
                setError('Not found.');
                setAgent(null);
            } else {
                setAgent(data);
            }
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { reload(); }, [project.id, agentId]);

    const startEdit = () => {
        if (!agent) return;
        setEditTitle(agent.title);
        setEditBody(agent.characteristics);
        setEditing(true);
    };

    const cancelEdit = () => setEditing(false);

    const saveEdit = async () => {
        if (!editTitle.trim()) {
            setError('Title is required.');
            return;
        }
        setSaving(true);
        setError(null);
        try {
            const updated = await api.updateAgent(project.id, agentId, {
                title: editTitle.trim(),
                characteristics: editBody
            });
            setAgent(updated);
            setEditing(false);
        } catch (err) {
            setError(err.message);
            onError?.(err.message);
        } finally {
            setSaving(false);
        }
    };

    if (loading && !agent) return <div className="empty-state subtle">Loading…</div>;
    if (error && !agent) return <div className="file-tree-error">{error}</div>;
    if (!agent) return null;

    return (
        <div className="document-tab">
            <header className="document-tab-header">
                <div className="document-tab-meta">
                    <span className="document-tab-eyebrow">Agent</span>
                    <span className="document-tab-date">added {formatTimestamp(agent.createdAt)}</span>
                    {agent.updatedAt && <span className="document-tab-date">updated {formatTimestamp(agent.updatedAt)}</span>}
                </div>
                {!editing ? (
                    <div className="agent-tab-titlebar">
                        <h1 className="document-tab-title">{agent.title}</h1>
                        <button type="button" className="btn btn-ghost btn-small" onClick={startEdit}>Edit</button>
                    </div>
                ) : (
                    <input
                        className="text-input"
                        value={editTitle}
                        onChange={(e) => setEditTitle(e.target.value)}
                        disabled={saving}
                    />
                )}
            </header>
            <div className="document-tab-body">
                {!editing && <MarkdownView source={agent.characteristics} />}
                {editing && (
                    <>
                        <textarea
                            className="text-input ticket-body-input"
                            value={editBody}
                            onChange={(e) => setEditBody(e.target.value)}
                            rows={20}
                            disabled={saving}
                        />
                        {error && <div className="modal-error">{error}</div>}
                        <div className="modal-actions">
                            <button type="button" className="btn btn-ghost" onClick={cancelEdit} disabled={saving}>Cancel</button>
                            <button type="button" className="btn btn-primary" onClick={saveEdit} disabled={saving || !editTitle.trim()}>
                                {saving ? 'Saving…' : 'Save'}
                            </button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}
