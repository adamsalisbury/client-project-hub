import { useEffect, useState } from 'react';
import { api } from '../api.js';
import CodeView from './CodeView.jsx';

export default function DiffTab({ project, path }) {
    const [diff, setDiff] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        setError(null);
        api.fileDiff(project.id, path)
            .then((data) => { if (!cancelled) setDiff(data); })
            .catch((err) => { if (!cancelled) setError(err.message); })
            .finally(() => { if (!cancelled) setLoading(false); });
        return () => { cancelled = true; };
    }, [project.id, path]);

    return (
        <div className="diff-tab">
            <header className="diff-tab-header">
                <code className="diff-tab-path">{path}</code>
                {diff && (
                    <span className={`diff-tab-status ${diff.hasChanges ? 'changed' : 'clean'}`}>
                        {diff.isUntracked
                            ? 'untracked (new file)'
                            : diff.hasChanges ? 'uncommitted changes' : 'no changes'}
                    </span>
                )}
            </header>
            <div className="diff-tab-body">
                {loading && <div className="empty-state subtle">Loading diff…</div>}
                {error && <div className="file-tree-error">{error}</div>}
                {diff && !diff.hasChanges && (
                    <div className="empty-state subtle">
                        This file has no uncommitted changes vs HEAD.
                    </div>
                )}
                {diff && diff.hasChanges && (
                    <CodeView code={diff.diff} language="diff" showLineNumbers={false} />
                )}
            </div>
        </div>
    );
}
