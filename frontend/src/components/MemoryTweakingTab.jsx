import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../api.js';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

function summarise(text, max = 80) {
    if (!text) return '';
    const oneLine = text.replace(/\s+/g, ' ').trim();
    return oneLine.length > max ? `${oneLine.slice(0, max - 1)}…` : oneLine;
}

function bytesOf(text) {
    try {
        return new TextEncoder().encode(text || '').length;
    } catch {
        return (text || '').length;
    }
}

function formatBytes(n) {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / (1024 * 1024)).toFixed(2)} MB`;
}

export default function MemoryTweakingTab({ project, onError, onProjectChanged }) {
    const [selection, setSelection] = useState(null);
    const [agents, setAgents] = useState([]);
    const [tickets, setTickets] = useState([]);
    const [projectKnowledge, setProjectKnowledge] = useState([]);
    const [clientKnowledge, setClientKnowledge] = useState([]);
    const [client, setClient] = useState(null);
    const [conversation, setConversation] = useState([]);
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [dirty, setDirty] = useState(false);

    const reload = useCallback(async () => {
        setLoading(true);
        try {
            const [sel, agentList, ticketList, kn, hist] = await Promise.all([
                api.getMemorySelection(project.id),
                api.listAgents(project.id),
                api.listTickets(project.id),
                api.listKnowledge(project.id),
                api.projectHistory(project.id)
            ]);
            setSelection(sel ?? defaultSelection());
            setAgents(agentList ?? []);
            setTickets(ticketList ?? []);
            setProjectKnowledge(kn ?? []);
            setConversation((hist?.messages ?? []).filter((m) => m.status === 'Completed' || m.status === 'Failed'));

            if (project.clientId) {
                const [c, cKn] = await Promise.all([
                    api.getClient(project.clientId),
                    api.listClientKnowledge(project.clientId)
                ]);
                setClient(c);
                setClientKnowledge(cKn ?? []);
            } else {
                setClient(null);
                setClientKnowledge([]);
            }

            setDirty(false);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoading(false);
        }
    }, [project.id, project.clientId, onError]);

    useEffect(() => { reload(); }, [reload]);

    const totalBytes = useMemo(() => {
        if (!selection) return 0;
        let total = 0;
        if (selection.includeProjectInfo) total += bytesOf(project.name) + bytesOf(project.workingDirectory);
        if (selection.includeClientInfo && client) total += bytesOf(client.name);

        const excludedAgents = new Set(selection.excludedAgentIds);
        for (const a of agents) {
            if (!excludedAgents.has(a.id)) total += bytesOf(a.title);
        }

        const excludedTickets = new Set(selection.excludedTicketIds);
        for (const t of tickets) {
            if (!excludedTickets.has(t.id)) total += bytesOf(t.title) + bytesOf(t.code);
        }

        const excludedPK = new Set(selection.excludedProjectKnowledgeIds);
        for (const k of projectKnowledge) {
            if (!excludedPK.has(k.id)) total += bytesOf(k.title);
        }

        const excludedCK = new Set(selection.excludedClientKnowledgeIds);
        for (const k of clientKnowledge) {
            if (!excludedCK.has(k.id)) total += bytesOf(k.title);
        }

        const excludedJobs = new Set(selection.excludedConversationJobIds);
        for (const m of conversation) {
            if (!excludedJobs.has(m.id)) total += bytesOf(m.message) + bytesOf(m.response || '');
        }

        return total;
    }, [selection, agents, tickets, projectKnowledge, clientKnowledge, conversation, client, project.name, project.workingDirectory]);

    const setIncluded = (kind, id, included) => {
        setSelection((prev) => {
            if (!prev) return prev;
            const key = excludedKey(kind);
            const current = new Set(prev[key]);
            if (included) {
                current.delete(id);
            } else {
                current.add(id);
            }
            return { ...prev, [key]: Array.from(current) };
        });
        setDirty(true);
    };

    const setFlag = (key, value) => {
        setSelection((prev) => prev ? { ...prev, [key]: value } : prev);
        setDirty(true);
    };

    const checkAll = (kind, items, setIncludedAll) => {
        setSelection((prev) => {
            if (!prev) return prev;
            const key = excludedKey(kind);
            const next = setIncludedAll ? [] : items.map((i) => i.id);
            return { ...prev, [key]: next };
        });
        setDirty(true);
    };

    const save = async () => {
        if (!selection) return;
        setSaving(true);
        try {
            const updated = await api.updateMemorySelection(project.id, selection);
            setSelection(updated);
            setDirty(false);
            onProjectChanged?.({ ...project });
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSaving(false);
        }
    };

    if (loading || !selection) {
        return <div className="empty-state subtle">Loading memory selection…</div>;
    }

    return (
        <div className="memory-tweak">
            <header className="memory-tweak-header">
                <div>
                    <h1 className="document-tab-title">Memory tweaking</h1>
                    <p className="modal-help">
                        Tick the items you want sent to Claude with each prompt. Untick anything that's
                        no longer relevant - useful for trimming an oversized history. Changes are saved
                        when you press <em>Save</em>.
                    </p>
                </div>
                <div className="memory-tweak-actions">
                    <span className="memory-tweak-bytes" title="Approximate size of selected items (excluding bodies)">
                        ≈ {formatBytes(totalBytes)} selected
                    </span>
                    <button type="button" className="btn btn-ghost" onClick={reload} disabled={saving}>Reset</button>
                    <button type="button" className="btn btn-primary" onClick={save} disabled={saving || !dirty}>
                        {saving ? 'Saving…' : (dirty ? 'Save' : 'Saved')}
                    </button>
                </div>
            </header>

            <section className="memory-tweak-section">
                <h2 className="memory-tweak-section-title">Project preamble</h2>
                <ul className="memory-tweak-list">
                    <li className="memory-tweak-row">
                        <label className="memory-tweak-label">
                            <input
                                type="checkbox"
                                checked={selection.includeProjectInfo}
                                onChange={(e) => setFlag('includeProjectInfo', e.target.checked)}
                            />
                            <span className="memory-tweak-name">Project name + working directory</span>
                            <span className="memory-tweak-detail">{project.name} · {project.workingDirectory}</span>
                        </label>
                    </li>
                    <li className="memory-tweak-row">
                        <label className={`memory-tweak-label ${!client ? 'disabled' : ''}`}>
                            <input
                                type="checkbox"
                                checked={selection.includeClientInfo && !!client}
                                onChange={(e) => setFlag('includeClientInfo', e.target.checked)}
                                disabled={!client}
                            />
                            <span className="memory-tweak-name">Client name</span>
                            <span className="memory-tweak-detail">{client?.name ?? 'No client attached'}</span>
                        </label>
                    </li>
                </ul>
            </section>

            <CheckboxGroup
                title="Agents"
                items={agents}
                excluded={selection.excludedAgentIds}
                renderName={(a) => a.title}
                renderDetail={(a) => `added ${formatTimestamp(a.createdAt)}`}
                onToggle={(id, on) => setIncluded('agents', id, on)}
                onCheckAll={(on) => checkAll('agents', agents, on)}
                empty="No agents defined."
            />

            <CheckboxGroup
                title="Client knowledge"
                items={clientKnowledge}
                excluded={selection.excludedClientKnowledgeIds}
                renderName={(k) => k.title}
                renderDetail={(k) => `created ${formatTimestamp(k.createdAt)}`}
                onToggle={(id, on) => setIncluded('clientKnowledge', id, on)}
                onCheckAll={(on) => checkAll('clientKnowledge', clientKnowledge, on)}
                empty={client ? 'No client knowledge yet.' : 'No client attached.'}
            />

            <CheckboxGroup
                title="Project knowledge"
                items={projectKnowledge}
                excluded={selection.excludedProjectKnowledgeIds}
                renderName={(k) => k.title}
                renderDetail={(k) => `created ${formatTimestamp(k.createdAt)}`}
                onToggle={(id, on) => setIncluded('projectKnowledge', id, on)}
                onCheckAll={(on) => checkAll('projectKnowledge', projectKnowledge, on)}
                empty="No project knowledge yet."
            />

            <CheckboxGroup
                title="Tickets"
                items={tickets}
                excluded={selection.excludedTicketIds}
                renderName={(t) => `${t.code} - ${t.title}`}
                renderDetail={(t) => `created ${formatTimestamp(t.createdAt)}`}
                onToggle={(id, on) => setIncluded('tickets', id, on)}
                onCheckAll={(on) => checkAll('tickets', tickets, on)}
                empty="No tickets."
            />

            <CheckboxGroup
                title="Conversation"
                items={conversation}
                excluded={selection.excludedConversationJobIds}
                renderName={(m) => summarise(m.message, 90)}
                renderDetail={(m) => {
                    const when = formatTimestamp(m.messageAt);
                    const summary = summarise(m.response, 60);
                    return summary ? `${when} · → ${summary}` : when;
                }}
                onToggle={(id, on) => setIncluded('conversation', id, on)}
                onCheckAll={(on) => checkAll('conversation', conversation, on)}
                empty="No completed conversation turns yet."
            />
        </div>
    );
}

function CheckboxGroup({ title, items, excluded, renderName, renderDetail, onToggle, onCheckAll, empty }) {
    const excludedSet = new Set(excluded);
    const includedCount = items.filter((i) => !excludedSet.has(i.id)).length;

    return (
        <section className="memory-tweak-section">
            <div className="memory-tweak-section-header">
                <h2 className="memory-tweak-section-title">{title}</h2>
                <span className="memory-tweak-section-count">
                    {includedCount}/{items.length} included
                </span>
                {items.length > 0 && (
                    <div className="memory-tweak-bulk">
                        <button type="button" className="btn-link" onClick={() => onCheckAll(true)}>All</button>
                        <span aria-hidden="true">·</span>
                        <button type="button" className="btn-link" onClick={() => onCheckAll(false)}>None</button>
                    </div>
                )}
            </div>
            {items.length === 0 && (
                <p className="empty-state subtle no-pad">{empty}</p>
            )}
            {items.length > 0 && (
                <ul className="memory-tweak-list">
                    {items.map((it) => {
                        const included = !excludedSet.has(it.id);
                        return (
                            <li key={it.id} className="memory-tweak-row">
                                <label className="memory-tweak-label">
                                    <input
                                        type="checkbox"
                                        checked={included}
                                        onChange={(e) => onToggle(it.id, e.target.checked)}
                                    />
                                    <span className="memory-tweak-name">{renderName(it)}</span>
                                    <span className="memory-tweak-detail">{renderDetail(it)}</span>
                                </label>
                            </li>
                        );
                    })}
                </ul>
            )}
        </section>
    );
}

function excludedKey(kind) {
    switch (kind) {
        case 'agents': return 'excludedAgentIds';
        case 'tickets': return 'excludedTicketIds';
        case 'projectKnowledge': return 'excludedProjectKnowledgeIds';
        case 'clientKnowledge': return 'excludedClientKnowledgeIds';
        case 'conversation': return 'excludedConversationJobIds';
        default: throw new Error(`unknown kind ${kind}`);
    }
}

function defaultSelection() {
    return {
        includeProjectInfo: true,
        includeClientInfo: true,
        excludedAgentIds: [],
        excludedTicketIds: [],
        excludedProjectKnowledgeIds: [],
        excludedClientKnowledgeIds: [],
        excludedConversationJobIds: []
    };
}
