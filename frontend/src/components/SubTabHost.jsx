import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api.js';
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
import PlanTab from './PlanTab.jsx';
import StepReviewTab from './StepReviewTab.jsx';

const POLL_INTERVAL_MS = 5000;

function hasPendingMessage(messages) {
    return Array.isArray(messages) && messages.some((m) => m.status === 'Queued' || m.status === 'Processing');
}

/**
 * Workspace-level dispatcher for sub-tabs. Loads the data each sub-tab
 * needs (chat history, tickets) and forwards it down. Polls the project
 * history for chat and diagnostics tabs while a job is pending.
 */
export default function SubTabHost({
    project,
    client,
    subKind,
    payload,
    onProjectChanged,
    onOpenSub,
    onError
}) {
    const [history, setHistory] = useState(null);
    const [loadingHistory, setLoadingHistory] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [tickets, setTickets] = useState([]);
    const pollRef = useRef(null);

    const wantsHistory = subKind === 'chat' || subKind === 'diagnostics';
    const wantsTickets = subKind === 'chat' || subKind === 'ticket';

    const loadHistory = useCallback(async () => {
        if (!project) return null;
        try {
            const data = await api.projectHistory(project.id);
            setHistory(data);
            return data;
        } catch (err) {
            onError?.(err.message);
            return null;
        }
    }, [project, onError]);

    const loadTickets = useCallback(async () => {
        if (!project) return;
        try {
            setTickets((await api.listTickets(project.id)) ?? []);
        } catch (err) {
            onError?.(err.message);
        }
    }, [project, onError]);

    useEffect(() => {
        if (!wantsHistory) return;
        setLoadingHistory(true);
        loadHistory().finally(() => setLoadingHistory(false));
    }, [wantsHistory, loadHistory]);

    useEffect(() => {
        if (wantsTickets) loadTickets();
    }, [wantsTickets, loadTickets]);

    useEffect(() => {
        if (!wantsHistory) return undefined;
        if (pollRef.current) clearInterval(pollRef.current);
        if (!hasPendingMessage(history?.messages)) return undefined;
        pollRef.current = setInterval(loadHistory, POLL_INTERVAL_MS);
        return () => {
            if (pollRef.current) {
                clearInterval(pollRef.current);
                pollRef.current = null;
            }
        };
    }, [wantsHistory, history, loadHistory]);

    const handleSubmitMessage = useCallback(async (message, kind) => {
        if (!project) return;
        setSubmitting(true);
        try {
            await api.submit(project.id, message, kind);
            await loadHistory();
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSubmitting(false);
        }
    }, [project, loadHistory, onError]);

    const openFile = (path) => onOpenSub('file', { path });
    const openDiff = (path) => onOpenSub('diff', { path });
    const openTicket = (ticket) => onOpenSub('ticket', { id: ticket.id });
    const openProjectKnowledge = (entry) => onOpenSub('knowledge-project', { id: entry.id });
    const openClientKnowledge = (entry) => entry?.clientId && onOpenSub('knowledge-client', { id: entry.id, clientId: entry.clientId });
    const openAgent = (agent) => onOpenSub('agent', { id: agent.id });
    const openAgents = () => onOpenSub('agents', {});
    const openMemoryTweak = () => onOpenSub('memory-tweak', {});
    const openDiagnostics = (message) => message?.id && onOpenSub('diagnostics', { jobId: message.id });

    if (!project) {
        return <div className="workspace-empty">Loading project…</div>;
    }

    switch (subKind) {
        case 'files':
            return (
                <FilesTab
                    project={project}
                    openFileTab={openFile}
                    openDiffTab={openDiff}
                    onError={onError}
                />
            );
        case 'chat':
            return (
                <ChatTab
                    project={project}
                    history={history}
                    loading={loadingHistory}
                    submitting={submitting}
                    onSubmit={handleSubmitMessage}
                    onViewFullMessage={openDiagnostics}
                />
            );
        case 'agents':
            return (
                <AgentsTab project={project} openAgent={openAgent} onError={onError} />
            );
        case 'plan':
            return (
                <PlanTab
                    project={project}
                    onOpenSub={onOpenSub}
                    onError={onError}
                />
            );
        case 'memory-tweak':
            return (
                <MemoryTweakingTab
                    project={project}
                    onError={onError}
                    onProjectChanged={onProjectChanged}
                />
            );
        case 'file':
            return (
                <FileTab
                    project={project}
                    path={payload.path}
                    openDiffTab={openDiff}
                    onError={onError}
                />
            );
        case 'diff':
            return <DiffTab project={project} path={payload.path} />;
        case 'ticket':
            return (
                <TicketTab
                    project={project}
                    ticketId={payload.id}
                    ticket={null}
                    tickets={tickets}
                    onError={onError}
                />
            );
        case 'agent':
            return (
                <AgentTab
                    project={project}
                    agentId={payload.id}
                    onError={onError}
                />
            );
        case 'knowledge-project':
            return (
                <KnowledgeTab
                    fetcher={() => api.getKnowledge(project.id, payload.id)}
                    eyebrow="Project knowledge"
                />
            );
        case 'knowledge-client':
            return (
                <KnowledgeTab
                    fetcher={() => api.getClientKnowledge(payload.clientId, payload.id)}
                    eyebrow="Client knowledge"
                />
            );
        case 'diagnostics':
            return (
                <DiagnosticsTab
                    message={(history?.messages ?? []).find((m) => m.id === payload.jobId)}
                />
            );
        case 'step-review':
            return (
                <StepReviewTab
                    project={project}
                    reviewId={payload.reviewId}
                    onOpenSub={onOpenSub}
                    onError={onError}
                />
            );
        default:
            return <div className="workspace-empty">Unknown sub-tab: {subKind}</div>;
    }
}
