import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api } from '../api.js';
import ProjectTabBar from './ProjectTabBar.jsx';
import InfoTab from './InfoTab.jsx';
import FilesTab from './FilesTab.jsx';
import ChatTab from './ChatTab.jsx';
import FileTab from './FileTab.jsx';
import DiffTab from './DiffTab.jsx';
import TicketTab from './TicketTab.jsx';
import KnowledgeTab from './KnowledgeTab.jsx';
import AgentsTab from './AgentsTab.jsx';
import AgentTab from './AgentTab.jsx';
import MemoryTweakingTab from './MemoryTweakingTab.jsx';
import DiagnosticsTab from './DiagnosticsTab.jsx';
import NewTicketDialog from './NewTicketDialog.jsx';
import { buildPath, pushUrl } from '../router.js';

const POLL_INTERVAL_MS = 5000;

const PERMANENT_TABS = [
    { id: 'info', kind: 'info', label: 'Info', closable: false },
    { id: 'files', kind: 'files', label: 'Files', closable: false },
    { id: 'agents', kind: 'agents', label: 'Agents', closable: false },
    { id: 'chat', kind: 'chat', label: 'Chat', closable: false }
];

function hasPendingMessage(messages) {
    return messages.some((m) => m.status === 'Queued' || m.status === 'Processing');
}

/**
 * Builds a tab descriptor from a route target. Returns null when the target
 * cannot be turned into a tab (e.g. unknown kind).
 */
function targetToTab(target, project) {
    if (!target) return null;
    switch (target.kind) {
        case 'info': return { id: 'info', kind: 'info', label: 'Info', closable: false };
        case 'files': return { id: 'files', kind: 'files', label: 'Files', closable: false };
        case 'agents': return { id: 'agents', kind: 'agents', label: 'Agents', closable: false };
        case 'chat': return { id: 'chat', kind: 'chat', label: 'Chat', closable: false };
        case 'memory-tweak': return {
            id: 'memory-tweak', kind: 'memory-tweak', label: 'Memory tweaking',
            tooltip: 'Choose what is sent to Claude', closable: true, payload: {}
        };
        case 'file': {
            const baseName = target.path?.split('/').pop() ?? target.path;
            return {
                id: `file:${target.path}`, kind: 'file', label: baseName,
                tooltip: target.path, payload: { path: target.path }, closable: true
            };
        }
        case 'diff': {
            const baseName = target.path?.split('/').pop() ?? target.path;
            return {
                id: `diff:${target.path}`, kind: 'diff', label: `Δ ${baseName}`,
                tooltip: `Local changes: ${target.path}`, payload: { path: target.path }, closable: true
            };
        }
        case 'ticket':
            return {
                id: `ticket:${target.id}`, kind: 'ticket', label: 'Ticket',
                payload: { ticketId: target.id }, closable: true
            };
        case 'agent':
            return {
                id: `agent:${target.id}`, kind: 'agent', label: 'Agent',
                payload: { agentId: target.id }, closable: true
            };
        case 'knowledge-project':
            return {
                id: `knowledge:${target.id}`, kind: 'knowledge-project', label: 'Knowledge',
                payload: { id: target.id }, closable: true
            };
        case 'knowledge-client':
            return {
                id: `client-knowledge:${target.id}`, kind: 'knowledge-client', label: 'Knowledge',
                payload: { id: target.id, clientId: target.clientId }, closable: true
            };
        case 'diagnostics':
            return {
                id: `diagnostics:${target.id}`, kind: 'diagnostics', label: 'Diagnostics',
                payload: { jobId: target.id }, closable: true
            };
        default:
            return null;
    }
}

export default function ProjectView({ project, initialTarget, onSwitchProject, onProjectChanged, onError }) {
    const [tabs, setTabs] = useState(() => {
        const fromTarget = targetToTab(initialTarget, project);
        if (!fromTarget) return PERMANENT_TABS;
        if (PERMANENT_TABS.some((t) => t.id === fromTarget.id)) return PERMANENT_TABS;
        return [...PERMANENT_TABS, fromTarget];
    });
    const [activeTabId, setActiveTabId] = useState(() => {
        const fromTarget = targetToTab(initialTarget, project);
        return fromTarget?.id ?? 'info';
    });

    const [tickets, setTickets] = useState([]);
    const [history, setHistory] = useState(null);
    const [loadingHistory, setLoadingHistory] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [showNewTicket, setShowNewTicket] = useState(false);

    const pollRef = useRef(null);
    const lastTargetRef = useRef(initialTarget);

    const loadHistory = useCallback(async () => {
        try {
            const data = await api.projectHistory(project.id);
            setHistory(data);
            return data;
        } catch (err) {
            onError?.(err.message);
            return null;
        }
    }, [project.id, onError]);

    const loadTickets = useCallback(async () => {
        try {
            const list = (await api.listTickets(project.id)) ?? [];
            setTickets(list);
        } catch (err) {
            onError?.(err.message);
            setTickets([]);
        }
    }, [project.id, onError]);

    useEffect(() => {
        setHistory(null);
        setTickets([]);
        setLoadingHistory(true);
        loadHistory().finally(() => setLoadingHistory(false));
        loadTickets();
    }, [project.id, loadHistory, loadTickets]);

    useEffect(() => {
        if (pollRef.current) {
            clearInterval(pollRef.current);
            pollRef.current = null;
        }
        if (!history?.messages) return;
        if (!hasPendingMessage(history.messages)) return;

        pollRef.current = setInterval(loadHistory, POLL_INTERVAL_MS);
        return () => {
            if (pollRef.current) {
                clearInterval(pollRef.current);
                pollRef.current = null;
            }
        };
    }, [history, loadHistory]);

    // Keep tab state aligned with prop-driven target (e.g. on browser back/forward).
    useEffect(() => {
        if (!initialTarget) return;
        if (initialTarget === lastTargetRef.current) return;
        lastTargetRef.current = initialTarget;
        const tab = targetToTab(initialTarget, project);
        if (!tab) return;
        setTabs((prev) => prev.some((t) => t.id === tab.id) ? prev : [...prev, tab]);
        setActiveTabId(tab.id);
    }, [initialTarget, project]);

    const navigateTo = useCallback((tab) => {
        setTabs((prev) => prev.some((t) => t.id === tab.id) ? prev : [...prev, tab]);
        setActiveTabId(tab.id);
        const path = buildPath(project.id, tab);
        pushUrl(path);
        lastTargetRef.current = null;
    }, [project.id]);

    const activateTab = useCallback((id) => {
        setActiveTabId(id);
        const tab = tabs.find((t) => t.id === id) ?? PERMANENT_TABS.find((t) => t.id === id);
        if (tab) {
            pushUrl(buildPath(project.id, tab));
        }
    }, [tabs, project.id]);

    const closeTab = useCallback((id) => {
        setTabs((prev) => {
            const idx = prev.findIndex((t) => t.id === id);
            if (idx < 0) return prev;
            const next = prev.filter((t) => t.id !== id);
            setActiveTabId((cur) => {
                if (cur !== id) return cur;
                const fallback = next[Math.max(0, idx - 1)] ?? next[0];
                if (fallback) {
                    pushUrl(buildPath(project.id, fallback));
                }
                return fallback?.id ?? 'info';
            });
            return next;
        });
    }, [project.id]);

    const openFileTab = useCallback((path) => {
        const baseName = path.split('/').pop();
        navigateTo({
            id: `file:${path}`, kind: 'file', label: baseName,
            tooltip: path, payload: { path }, closable: true
        });
    }, [navigateTo]);

    const openDiffTab = useCallback((path) => {
        const baseName = path.split('/').pop();
        navigateTo({
            id: `diff:${path}`, kind: 'diff', label: `Δ ${baseName}`,
            tooltip: `Local changes: ${path}`, payload: { path }, closable: true
        });
    }, [navigateTo]);

    const openTicketTab = useCallback((ticket) => {
        navigateTo({
            id: `ticket:${ticket.id}`, kind: 'ticket',
            label: ticket.code || ticket.title || 'Ticket', tooltip: ticket.title,
            payload: { ticketId: ticket.id, ticket }, closable: true
        });
    }, [navigateTo]);

    const openProjectKnowledgeTab = useCallback((entry) => {
        navigateTo({
            id: `knowledge:${entry.id}`, kind: 'knowledge-project',
            label: entry.title, tooltip: entry.title,
            payload: { id: entry.id }, closable: true
        });
    }, [navigateTo]);

    const openClientKnowledgeTab = useCallback((entry) => {
        if (!entry.clientId) return;
        navigateTo({
            id: `client-knowledge:${entry.id}`, kind: 'knowledge-client',
            label: entry.title, tooltip: entry.title,
            payload: { id: entry.id, clientId: entry.clientId }, closable: true
        });
    }, [navigateTo]);

    const openAgentTab = useCallback((agent) => {
        navigateTo({
            id: `agent:${agent.id}`, kind: 'agent',
            label: agent.title, tooltip: agent.title,
            payload: { agentId: agent.id }, closable: true
        });
    }, [navigateTo]);

    const openDiagnosticsTab = useCallback((message) => {
        if (!message?.id) return;
        navigateTo({
            id: `diagnostics:${message.id}`, kind: 'diagnostics',
            label: 'Diagnostics', tooltip: 'Full prompt sent to Claude',
            payload: { jobId: message.id }, closable: true
        });
    }, [navigateTo]);

    const openMemoryTweakTab = useCallback(() => {
        navigateTo({
            id: 'memory-tweak', kind: 'memory-tweak', label: 'Memory tweaking',
            tooltip: 'Choose what is sent to Claude', payload: {}, closable: true
        });
    }, [navigateTo]);

    const openAgentsTab = useCallback(() => activateTab('agents'), [activateTab]);

    const handleSubmitMessage = useCallback(async (message, kind) => {
        setSubmitting(true);
        try {
            await api.submit(project.id, message, kind);
            await loadHistory();
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSubmitting(false);
        }
    }, [project.id, loadHistory, onError]);

    const handleCreateTicket = useCallback(async (ticket) => {
        await api.createTicket(project.id, ticket);
        setShowNewTicket(false);
        await loadTickets();
    }, [project.id, loadTickets]);

    const activeTab = useMemo(() => tabs.find((t) => t.id === activeTabId) ?? tabs[0], [tabs, activeTabId]);

    return (
        <div className="project-view">
            <div className="project-view-topbar">
                <div className="project-view-identity">
                    <button
                        type="button"
                        className="btn btn-ghost project-view-switch"
                        onClick={onSwitchProject}
                        title="Switch project"
                    >
                        <span aria-hidden="true">↩</span>
                        <span>Switch project</span>
                    </button>
                    <div className="project-view-name">{project.name}</div>
                    <code className="project-view-cwd" title={project.workingDirectory}>
                        {project.workingDirectory}
                    </code>
                </div>
            </div>

            <ProjectTabBar
                tabs={tabs}
                activeId={activeTabId}
                onActivate={activateTab}
                onClose={closeTab}
            />

            <div className="project-view-tabbody">
                {activeTab?.kind === 'info' && (
                    <InfoTab
                        project={project}
                        tickets={tickets}
                        openTicket={openTicketTab}
                        openProjectKnowledge={openProjectKnowledgeTab}
                        openClientKnowledge={openClientKnowledgeTab}
                        openProjectKnowledgeById={openProjectKnowledgeTab}
                        openClientKnowledgeById={openClientKnowledgeTab}
                        openAgents={openAgentsTab}
                        openAgent={openAgentTab}
                        openMemoryTweak={openMemoryTweakTab}
                        onProjectChanged={onProjectChanged}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'files' && (
                    <FilesTab
                        project={project}
                        openFileTab={openFileTab}
                        openDiffTab={openDiffTab}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'chat' && (
                    <ChatTab
                        project={project}
                        history={history}
                        loading={loadingHistory}
                        submitting={submitting}
                        onSubmit={handleSubmitMessage}
                        onViewFullMessage={openDiagnosticsTab}
                    />
                )}
                {activeTab?.kind === 'file' && (
                    <FileTab
                        project={project}
                        path={activeTab.payload.path}
                        openDiffTab={openDiffTab}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'diff' && (
                    <DiffTab project={project} path={activeTab.payload.path} />
                )}
                {activeTab?.kind === 'ticket' && (
                    <TicketTab
                        project={project}
                        ticketId={activeTab.payload.ticketId}
                        ticket={activeTab.payload.ticket}
                        tickets={tickets}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'knowledge-project' && (
                    <KnowledgeTab
                        fetcher={() => api.getKnowledge(project.id, activeTab.payload.id)}
                        eyebrow="Project knowledge"
                    />
                )}
                {activeTab?.kind === 'knowledge-client' && (
                    <KnowledgeTab
                        fetcher={() => api.getClientKnowledge(activeTab.payload.clientId, activeTab.payload.id)}
                        eyebrow="Client knowledge"
                    />
                )}
                {activeTab?.kind === 'agents' && (
                    <AgentsTab
                        project={project}
                        openAgent={openAgentTab}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'agent' && (
                    <AgentTab
                        project={project}
                        agentId={activeTab.payload.agentId}
                        onError={onError}
                    />
                )}
                {activeTab?.kind === 'memory-tweak' && (
                    <MemoryTweakingTab
                        project={project}
                        onError={onError}
                        onProjectChanged={onProjectChanged}
                    />
                )}
                {activeTab?.kind === 'diagnostics' && (
                    <DiagnosticsTab
                        message={(history?.messages ?? []).find((m) => m.id === activeTab.payload.jobId)}
                    />
                )}
            </div>

            {showNewTicket && (
                <NewTicketDialog
                    projectId={project.id}
                    onCreate={handleCreateTicket}
                    onCancel={() => setShowNewTicket(false)}
                />
            )}
        </div>
    );
}
