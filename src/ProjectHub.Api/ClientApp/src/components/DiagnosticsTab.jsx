function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
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

export default function DiagnosticsTab({ message }) {
    if (!message) {
        return <div className="empty-state subtle">Message not found.</div>;
    }

    const promptMissing = message.status === 'Queued';
    const promptUnavailable = !message.prompt && !promptMissing;

    return (
        <div className="diagnostics-tab">
            <header className="diagnostics-header">
                <h1 className="document-tab-title">Diagnostics</h1>
                <p className="modal-help">
                    Everything that was sent to Claude Code for this message — the
                    fully rendered prompt including agent personas, project context,
                    prior conversation turns, and the user message itself.
                </p>
            </header>

            <section className="diagnostics-section">
                <h2 className="diagnostics-section-title">Message</h2>
                <dl className="info-grid">
                    <dt>Kind</dt>
                    <dd>{message.kind}</dd>
                    <dt>Status</dt>
                    <dd>{message.status}</dd>
                    <dt>Sent</dt>
                    <dd>{formatTimestamp(message.messageAt)}</dd>
                    {message.responseAt && (<>
                        <dt>Completed</dt>
                        <dd>{formatTimestamp(message.responseAt)}</dd>
                    </>)}
                    {message.durationMs != null && (<>
                        <dt>Duration</dt>
                        <dd>{formatDuration(message.durationMs)}</dd>
                    </>)}
                    {message.exitCode != null && (<>
                        <dt>Exit code</dt>
                        <dd>{message.exitCode}</dd>
                    </>)}
                    <dt>Job id</dt>
                    <dd><code className="info-mono">{message.id}</code></dd>
                </dl>
            </section>

            <section className="diagnostics-section">
                <h2 className="diagnostics-section-title">User message (verbatim)</h2>
                <pre className="diagnostics-pre">{message.message}</pre>
            </section>

            <section className="diagnostics-section">
                <h2 className="diagnostics-section-title">Full prompt sent to Claude</h2>
                {promptMissing && (
                    <p className="empty-state subtle no-pad">
                        This job is still queued — the prompt is rendered just
                        before Claude is invoked.
                    </p>
                )}
                {promptUnavailable && (
                    <p className="empty-state subtle no-pad">
                        No prompt was captured for this job. (Older jobs created
                        before the diagnostics feature was added won't have one.)
                    </p>
                )}
                {message.prompt && (
                    <pre className="diagnostics-pre diagnostics-prompt">{message.prompt}</pre>
                )}
            </section>

            {message.response && (
                <section className="diagnostics-section">
                    <h2 className="diagnostics-section-title">Claude's response</h2>
                    <pre className="diagnostics-pre">{message.response}</pre>
                </section>
            )}

            {message.error && (
                <section className="diagnostics-section">
                    <h2 className="diagnostics-section-title">Error</h2>
                    <pre className="diagnostics-pre">{message.error}</pre>
                </section>
            )}
        </div>
    );
}
