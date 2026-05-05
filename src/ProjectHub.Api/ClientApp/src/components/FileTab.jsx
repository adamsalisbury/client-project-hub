import { useEffect, useState } from 'react';
import { api } from '../api.js';
import CodeView from './CodeView.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

function formatSize(bytes) {
    if (bytes == null) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function FileTab({ project, path, onError, openDiffTab }) {
    const [content, setContent] = useState(null);
    const [history, setHistory] = useState(null);
    const [loadingContent, setLoadingContent] = useState(false);
    const [loadingHistory, setLoadingHistory] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        let cancelled = false;
        setLoadingContent(true);
        setError(null);
        api.readProjectFile(project.id, path)
            .then((data) => {
                if (cancelled) return;
                if (!data) {
                    setError('File not found.');
                    setContent(null);
                } else {
                    setContent(data);
                }
            })
            .catch((err) => { if (!cancelled) { setError(err.message); setContent(null); } })
            .finally(() => { if (!cancelled) setLoadingContent(false); });

        setLoadingHistory(true);
        api.fileHistory(project.id, path)
            .then((data) => { if (!cancelled) setHistory(data); })
            .catch((err) => { if (!cancelled) onError?.(err.message); })
            .finally(() => { if (!cancelled) setLoadingHistory(false); });

        return () => { cancelled = true; };
    }, [project.id, path, onError]);

    return (
        <div className="file-tab">
            <aside className="file-tab-side">
                <div className="file-tab-side-section">
                    <div className="file-tab-side-label">Path</div>
                    <code className="file-tab-side-path">{path}</code>
                </div>
                <div className="file-tab-side-section">
                    <div className="file-tab-side-label">Size</div>
                    <div className="file-tab-side-value">{content ? formatSize(content.size) : '-'}</div>
                </div>
                <div className="file-tab-side-section">
                    <button
                        type="button"
                        className="btn btn-ghost file-tab-action"
                        onClick={() => openDiffTab(path)}
                    >
                        <span aria-hidden="true">⇄</span>
                        <span>Show local changes</span>
                    </button>
                </div>
                <div className="file-tab-side-section grow">
                    <div className="file-tab-side-label">Git history</div>
                    {loadingHistory && <div className="empty-state subtle no-pad">Loading…</div>}
                    {!loadingHistory && history && history.commits.length === 0 && (
                        <div className="empty-state subtle no-pad">No commits.</div>
                    )}
                    {history && history.commits.length > 0 && (
                        <ul className="commit-list">
                            {history.commits.map((c) => (
                                <li key={c.sha} className="commit-item" title={c.subject}>
                                    <div className="commit-row1">
                                        <code className="commit-sha">{c.shortSha}</code>
                                        <span className="commit-date">{formatTimestamp(c.date)}</span>
                                    </div>
                                    <div className="commit-author">{c.author}</div>
                                    <div className="commit-subject">{c.subject}</div>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            </aside>
            <main className="file-tab-main">
                {loadingContent && <div className="empty-state subtle">Loading…</div>}
                {error && <div className="file-tree-error">{error}</div>}
                {content && content.isBinary && (
                    <div className="empty-state subtle">Binary file; content not shown.</div>
                )}
                {content && content.truncated && (
                    <div className="empty-state subtle">File too large for preview.</div>
                )}
                {content && !content.isBinary && !content.truncated && (
                    <CodeView code={content.content || ''} path={path} />
                )}
            </main>
        </div>
    );
}
