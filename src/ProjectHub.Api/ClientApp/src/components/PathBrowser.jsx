import { useCallback, useEffect, useRef, useState } from 'react';
import { api } from '../api.js';

function pathSegments(path) {
    if (!path) return [];
    const isWindows = /^[A-Za-z]:[\\/]/.test(path);
    const sep = isWindows ? '\\' : '/';
    const parts = path.split(/[\\/]+/).filter(Boolean);
    const segments = [];
    let acc = isWindows ? '' : '/';
    if (isWindows && parts.length > 0) {
        acc = parts[0] + sep;
        segments.push({ label: parts[0] + sep, path: acc });
        for (let i = 1; i < parts.length; i++) {
            acc = acc + parts[i] + (i < parts.length - 1 ? sep : '');
            segments.push({ label: parts[i], path: acc });
        }
    } else {
        segments.push({ label: '/', path: '/' });
        for (let i = 0; i < parts.length; i++) {
            acc = acc + (acc.endsWith('/') ? '' : '/') + parts[i];
            segments.push({ label: parts[i], path: acc });
        }
    }
    return segments;
}

export default function PathBrowser({ initialPath, onSelect, onCancel }) {
    const [listing, setListing] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [manualPath, setManualPath] = useState('');
    const initialNavigated = useRef(false);

    const navigate = useCallback(async (path) => {
        setLoading(true);
        setError(null);
        try {
            const data = await api.browseFilesystem(path);
            if (!data) {
                setError(`Directory not found: ${path}`);
            } else {
                setListing(data);
                setManualPath(data.path);
            }
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        if (initialNavigated.current) return;
        initialNavigated.current = true;
        navigate(initialPath || null);
    }, [initialPath, navigate]);

    const handleManualSubmit = (e) => {
        e.preventDefault();
        if (manualPath.trim()) {
            navigate(manualPath.trim());
        }
    };

    const choose = () => {
        if (listing?.path) onSelect(listing.path);
    };

    const segments = listing ? pathSegments(listing.path) : [];

    return (
        <div className="path-browser">
            <form className="path-input-row" onSubmit={handleManualSubmit}>
                <input
                    type="text"
                    className="text-input path-input"
                    placeholder="Type a path or use the browser below"
                    value={manualPath}
                    onChange={(e) => setManualPath(e.target.value)}
                />
                <button type="submit" className="btn btn-ghost" disabled={loading}>Go</button>
            </form>

            <div className="path-breadcrumbs" aria-label="Path">
                {listing && (
                    <>
                        <button type="button" className="crumb" onClick={() => navigate(listing.homePath)} title="Home">~</button>
                        {segments.map((seg, idx) => (
                            <span key={idx} className="crumb-wrapper">
                                <span className="crumb-sep" aria-hidden="true">/</span>
                                <button
                                    type="button"
                                    className="crumb"
                                    onClick={() => navigate(seg.path)}
                                    disabled={idx === segments.length - 1}
                                >
                                    {seg.label}
                                </button>
                            </span>
                        ))}
                    </>
                )}
            </div>

            <div className="path-listing" aria-busy={loading}>
                {error && <div className="path-error">{error}</div>}
                {loading && !listing && <div className="path-loading">Loading…</div>}

                {listing && (
                    <ul className="path-entries">
                        {listing.parentPath && (
                            <li>
                                <button
                                    type="button"
                                    className="path-entry directory"
                                    onClick={() => navigate(listing.parentPath)}
                                >
                                    <span className="path-icon" aria-hidden="true">↩</span>
                                    <span className="path-name">..</span>
                                </button>
                            </li>
                        )}
                        {listing.entries.length === 0 && (
                            <li className="path-empty">No entries.</li>
                        )}
                        {listing.entries.map((entry) => (
                            <li key={entry.path}>
                                <button
                                    type="button"
                                    className={`path-entry ${entry.isDirectory ? 'directory' : 'file'}`}
                                    onClick={() => entry.isDirectory && navigate(entry.path)}
                                    disabled={!entry.isDirectory}
                                    title={entry.isDirectory ? entry.path : 'Files cannot be selected as a working directory'}
                                >
                                    <span className="path-icon" aria-hidden="true">{entry.isDirectory ? '📁' : '·'}</span>
                                    <span className="path-name">{entry.name}</span>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </div>

            <div className="path-actions">
                <button type="button" className="btn btn-ghost" onClick={onCancel}>Cancel</button>
                <button type="button" className="btn btn-primary" onClick={choose} disabled={!listing?.path}>
                    Use this folder
                </button>
            </div>
        </div>
    );
}
