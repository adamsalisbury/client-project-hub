import { useEffect, useRef, useState } from 'react';
import ContextMenu from './ContextMenu.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString(undefined, {
            hour: '2-digit',
            minute: '2-digit',
            month: 'short',
            day: 'numeric'
        });
    } catch {
        return iso;
    }
}

function formatDuration(ms) {
    if (ms == null) return '';
    if (ms < 1000) return `${ms}ms`;
    const s = ms / 1000;
    if (s < 60) return `${s.toFixed(1)}s`;
    const m = Math.floor(s / 60);
    const r = Math.round(s - m * 60);
    return `${m}m ${r}s`;
}

function StatusBadge({ status }) {
    return (
        <span className="status-badge" data-status={status}>
            <span className="dot" />
            {status}
        </span>
    );
}

function KindBadge({ kind }) {
    const label = kind === 'Edit' ? 'edit' : 'chat';
    const icon = kind === 'Edit' ? '✎' : '💬';
    return (
        <span className="kind-badge" data-kind={kind}>
            <span aria-hidden="true">{icon}</span>
            {label}
        </span>
    );
}

function FilesChangedBlock({ files }) {
    if (!files || files.length === 0) return null;
    return (
        <div className="files-changed" aria-label="Files changed">
            <div className="files-changed-header">
                <span className="files-changed-count">{files.length}</span>
                file{files.length === 1 ? '' : 's'} changed
            </div>
            <ul className="files-changed-list">
                {files.map((path) => (
                    <li key={path} className="files-changed-item">
                        <span className="files-changed-icon" aria-hidden="true">📄</span>
                        <code>{path}</code>
                    </li>
                ))}
            </ul>
        </div>
    );
}

function UserMessage({ message, onContextMenu }) {
    return (
        <div className="chat-row chat-row-user">
            <div
                className="chat-bubble chat-bubble-user"
                onContextMenu={(e) => onContextMenu?.(e, message)}
            >
                <div className="chat-bubble-body">{message.message}</div>
                <div className="chat-bubble-foot">
                    <KindBadge kind={message.kind} />
                    <span className="chat-bubble-time">{formatTimestamp(message.messageAt)}</span>
                </div>
            </div>
        </div>
    );
}

function AssistantMessage({ message }) {
    const failed = message.status === 'Failed';
    const pending = message.status === 'Queued' || message.status === 'Processing';

    return (
        <div className="chat-row chat-row-assistant">
            <div className={`chat-bubble chat-bubble-assistant ${failed ? 'failed' : ''} ${pending ? 'pending' : ''}`}>
                {message.status === 'Queued' && (
                    <div className="chat-bubble-body subtle">Queued. Waiting for the worker.</div>
                )}
                {message.status === 'Processing' && (
                    <div className="chat-bubble-body subtle">Running through the AI.</div>
                )}
                {message.status === 'Completed' && (
                    <div className="chat-bubble-body">{message.response || ''}</div>
                )}
                {failed && (
                    <div className="chat-bubble-body">
                        <strong>Failed.</strong> {message.error || message.response || 'No error message.'}
                    </div>
                )}

                {(message.status === 'Completed' || failed) && (
                    <FilesChangedBlock files={message.filesChanged} />
                )}

                <div className="chat-bubble-foot">
                    <StatusBadge status={message.status} />
                    {message.responseAt && (
                        <span className="chat-bubble-time">{formatTimestamp(message.responseAt)}</span>
                    )}
                    {message.durationMs != null && (
                        <span className="chat-bubble-time">{formatDuration(message.durationMs)}</span>
                    )}
                </div>
            </div>
        </div>
    );
}

export default function ChatHistory({ messages, loading, onViewFullMessage }) {
    const scrollRef = useRef(null);
    const [contextMenu, setContextMenu] = useState(null);

    useEffect(() => {
        scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
    }, [messages]);

    const handleContextMenu = (event, message) => {
        if (!onViewFullMessage) return;
        event.preventDefault();
        setContextMenu({ x: event.clientX, y: event.clientY, message });
    };

    return (
        <div className="chat" ref={scrollRef}>
            {loading && messages.length === 0 && (
                <div className="empty-state subtle">Loading history.</div>
            )}

            {!loading && messages.length === 0 && (
                <div className="empty-state subtle">No messages yet. Say hello below.</div>
            )}

            {messages.map((m) => (
                <div key={m.id} className="chat-turn">
                    <UserMessage message={m} onContextMenu={handleContextMenu} />
                    <AssistantMessage message={m} />
                </div>
            ))}

            {contextMenu && (
                <ContextMenu
                    x={contextMenu.x}
                    y={contextMenu.y}
                    items={[{
                        icon: '🔍',
                        label: 'View full message',
                        onClick: () => onViewFullMessage(contextMenu.message)
                    }]}
                    onClose={() => setContextMenu(null)}
                />
            )}
        </div>
    );
}
