import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';

function formatSize(bytes) {
    if (bytes == null) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default function FileTree({ projectId, currentPath, onNavigate, onSelectFile, selectedFile, onContextMenu }) {
    const [listing, setListing] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const load = useCallback(async (path) => {
        if (!projectId) return;
        setLoading(true);
        setError(null);
        try {
            const data = await api.listProjectFiles(projectId, path || null);
            if (!data) {
                setError('Not found.');
                setListing(null);
            } else {
                setListing(data);
            }
        } catch (err) {
            setError(err.message);
            setListing(null);
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        load(currentPath);
    }, [currentPath, load]);

    if (!projectId) {
        return <div className="file-tree empty">No project selected.</div>;
    }

    const segments = currentPath ? currentPath.split('/').filter(Boolean) : [];

    return (
        <div className="file-tree">
            <div className="file-tree-breadcrumbs" aria-label="Path within project">
                <button
                    type="button"
                    className="crumb"
                    onClick={() => onNavigate('')}
                    disabled={!currentPath}
                    title="Project root"
                >
                    /
                </button>
                {segments.map((seg, idx) => {
                    const fullPath = segments.slice(0, idx + 1).join('/');
                    const isLast = idx === segments.length - 1;
                    return (
                        <span key={fullPath} className="crumb-wrapper">
                            <span className="crumb-sep" aria-hidden="true">/</span>
                            <button
                                type="button"
                                className="crumb"
                                onClick={() => onNavigate(fullPath)}
                                disabled={isLast}
                            >
                                {seg}
                            </button>
                        </span>
                    );
                })}
            </div>

            <div className="file-tree-listing">
                {error && <div className="file-tree-error">{error}</div>}
                {loading && !listing && <div className="file-tree-loading">Loading…</div>}

                {listing && (
                    <ul className="file-tree-entries">
                        {listing.parentRelativePath !== null && (
                            <li>
                                <button
                                    type="button"
                                    className="file-entry directory"
                                    onClick={() => onNavigate(listing.parentRelativePath)}
                                >
                                    <span className="file-entry-icon" aria-hidden="true">↩</span>
                                    <span className="file-entry-name">..</span>
                                </button>
                            </li>
                        )}
                        {listing.entries.length === 0 && (
                            <li className="file-tree-empty">Empty.</li>
                        )}
                        {listing.entries.map((entry) => {
                            const isSelected = !entry.isDirectory && entry.relativePath === selectedFile;
                            return (
                                <li key={entry.relativePath}>
                                    <button
                                        type="button"
                                        className={`file-entry ${entry.isDirectory ? 'directory' : 'file'} ${isSelected ? 'selected' : ''}`}
                                        onClick={() => entry.isDirectory ? onNavigate(entry.relativePath) : onSelectFile(entry.relativePath)}
                                        onContextMenu={(e) => onContextMenu?.(e, entry)}
                                    >
                                        <span className="file-entry-icon" aria-hidden="true">
                                            {entry.isDirectory ? '📁' : '📄'}
                                        </span>
                                        <span className="file-entry-name">{entry.name}</span>
                                        {!entry.isDirectory && entry.size != null && (
                                            <span className="file-entry-size">{formatSize(entry.size)}</span>
                                        )}
                                    </button>
                                </li>
                            );
                        })}
                    </ul>
                )}
            </div>
        </div>
    );
}
