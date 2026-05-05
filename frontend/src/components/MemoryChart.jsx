function formatBytes(n) {
    if (n == null) return '-';
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / (1024 * 1024)).toFixed(2)} MB`;
}

function pct(num, den) {
    if (!den) return 0;
    return Math.max(0, Math.min(100, (num / den) * 100));
}

export default function MemoryChart({ usage }) {
    if (!usage) {
        return <div className="empty-state subtle no-pad">Loading…</div>;
    }

    const segments = [
        { key: 'agents', label: 'Agents', bytes: usage.agentBytes, color: 'var(--accent-strong)' },
        { key: 'conversation', label: 'Conversation', bytes: usage.conversationBytes, color: 'var(--user)' },
        { key: 'tickets', label: 'Tickets', bytes: usage.ticketBytes, color: 'var(--accent)' },
        { key: 'project-kn', label: 'Project knowledge', bytes: usage.projectKnowledgeBytes, color: 'var(--success)' },
        { key: 'client-kn', label: 'Client knowledge', bytes: usage.clientKnowledgeBytes, color: 'var(--warning)' },
        { key: 'project-info', label: 'Project info', bytes: usage.projectInfoBytes, color: 'var(--text-muted)' }
    ].filter((s) => s.bytes > 0);

    const usedPct = pct(usage.totalBytes, usage.maxBytes);

    return (
        <div className="memory-chart">
            <div className="memory-chart-row">
                <div className="memory-chart-bar" role="progressbar" aria-valuenow={Math.round(usedPct)} aria-valuemin={0} aria-valuemax={100}>
                    {segments.map((s) => (
                        <div
                            key={s.key}
                            className="memory-chart-seg"
                            style={{
                                width: `${pct(s.bytes, usage.maxBytes)}%`,
                                background: s.color
                            }}
                            title={`${s.label}: ${formatBytes(s.bytes)}`}
                        />
                    ))}
                </div>
                <div className="memory-chart-total">
                    {formatBytes(usage.totalBytes)} / {formatBytes(usage.maxBytes)} ({usedPct.toFixed(1)}%)
                </div>
            </div>

            <ul className="memory-chart-legend">
                {segments.map((s) => (
                    <li key={s.key} className="memory-chart-legend-item">
                        <span className="memory-chart-swatch" style={{ background: s.color }} aria-hidden="true" />
                        <span className="memory-chart-legend-label">{s.label}</span>
                        <span className="memory-chart-legend-bytes">{formatBytes(s.bytes)}</span>
                    </li>
                ))}
                {segments.length === 0 && (
                    <li className="memory-chart-legend-item subtle">No memory used yet.</li>
                )}
            </ul>
        </div>
    );
}
