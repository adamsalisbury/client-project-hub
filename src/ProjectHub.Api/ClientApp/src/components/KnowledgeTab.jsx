import { useEffect, useState } from 'react';
import MarkdownView from './MarkdownView.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

export default function KnowledgeTab({ fetcher, eyebrow = 'Knowledge' }) {
    const [entry, setEntry] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        setError(null);
        fetcher()
            .then((data) => {
                if (cancelled) return;
                if (!data) setError('Not found.');
                else setEntry(data);
            })
            .catch((err) => { if (!cancelled) setError(err.message); })
            .finally(() => { if (!cancelled) setLoading(false); });
        return () => { cancelled = true; };
    }, [fetcher]);

    if (loading) return <div className="empty-state subtle">Loading…</div>;
    if (error) return <div className="file-tree-error">{error}</div>;
    if (!entry) return null;

    return (
        <div className="document-tab">
            <header className="document-tab-header">
                <div className="document-tab-meta">
                    <span className="document-tab-eyebrow">{eyebrow}</span>
                    <span className="document-tab-date">created {formatTimestamp(entry.createdAt)}</span>
                </div>
                <h1 className="document-tab-title">{entry.title}</h1>
            </header>
            <div className="document-tab-body">
                <MarkdownView source={entry.body} />
            </div>
        </div>
    );
}
