import { useEffect, useState } from 'react';
import { api } from '../api.js';
import CodeView from './CodeView.jsx';

function formatSize(bytes) {
    if (bytes == null) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function FileViewer({ projectId, path, onBack }) {
    const [content, setContent] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (!projectId || !path) {
            setContent(null);
            return;
        }
        let cancelled = false;
        setLoading(true);
        setError(null);
        api.readProjectFile(projectId, path)
            .then((data) => {
                if (cancelled) return;
                if (!data) {
                    setError('Not found.');
                    setContent(null);
                } else {
                    setContent(data);
                }
            })
            .catch((err) => {
                if (cancelled) return;
                setError(err.message);
                setContent(null);
            })
            .finally(() => { if (!cancelled) setLoading(false); });
        return () => { cancelled = true; };
    }, [projectId, path]);

    if (!path) {
        return (
            <div className="file-viewer empty">
                <p className="empty-state subtle">Pick a file from the tree to view it here.</p>
            </div>
        );
    }

    return (
        <div className="file-viewer">
            <div className="file-viewer-header">
                {onBack && (
                    <button type="button" className="btn btn-ghost file-viewer-back" onClick={onBack} aria-label="Back to tree">
                        <span aria-hidden="true">←</span>
                        <span className="btn-label">Tree</span>
                    </button>
                )}
                <code className="file-viewer-path">{path}</code>
                {content && <span className="file-viewer-size">{formatSize(content.size)}</span>}
            </div>

            <div className="file-viewer-body">
                {loading && <div className="file-tree-loading">Loading…</div>}
                {error && <div className="file-tree-error">{error}</div>}

                {content && content.isBinary && (
                    <div className="empty-state subtle">
                        Binary file ({formatSize(content.size)}); content not shown.
                    </div>
                )}
                {content && content.truncated && (
                    <div className="empty-state subtle">
                        File is larger than the 1 MB preview cap; content not shown.
                    </div>
                )}
                {content && !content.isBinary && !content.truncated && (
                    <CodeView code={content.content || ''} path={path} />
                )}
            </div>
        </div>
    );
}
