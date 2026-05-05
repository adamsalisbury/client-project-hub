import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';

/**
 * Sub-tab body for a single plan-step's review. Lists every file the AI
 * touched, lets the user click through to the diff, and per-file commit /
 * rollback. Spawned automatically by the project hub when a plan step
 * finishes; can also be reopened from the project's recent-reviews list.
 */
export default function StepReviewTab({ project, reviewId, onOpenSub, onError }) {
    const [review, setReview] = useState(null);
    const [loading, setLoading] = useState(true);
    const [busyPath, setBusyPath] = useState(null);
    const [commitMessage, setCommitMessage] = useState('');

    const load = useCallback(async () => {
        setLoading(true);
        try {
            setReview(await api.getStepReview(project.id, reviewId));
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoading(false);
        }
    }, [project.id, reviewId, onError]);

    useEffect(() => { load(); }, [load]);

    const commit = async (path) => {
        setBusyPath(path);
        try {
            const updated = await api.commitReviewFile(project.id, reviewId, path, commitMessage || null);
            setReview(updated);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusyPath(null);
        }
    };

    const rollback = async (path) => {
        if (!window.confirm(`Roll back '${path}'? Local changes will be lost.`)) return;
        setBusyPath(path);
        try {
            const updated = await api.rollbackReviewFile(project.id, reviewId, path);
            setReview(updated);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusyPath(null);
        }
    };

    if (loading) return <div className="workspace-empty">Loading step review…</div>;
    if (!review) return <div className="workspace-empty">Review not found.</div>;

    return (
        <div className="step-review-tab">
            <header className="plan-tab-head">
                <h1>Step review</h1>
                <span className="step-review-meta">
                    {new Date(review.createdAt).toLocaleString()} — {review.files.length} file(s)
                </span>
            </header>

            <label className="field-label" htmlFor="commit-message">Commit message (used when committing files)</label>
            <input
                id="commit-message"
                type="text"
                className="text-input"
                placeholder={`step ${review.stepId.slice(0, 6)}`}
                value={commitMessage}
                onChange={(e) => setCommitMessage(e.target.value)}
            />

            {review.files.length === 0 && (
                <p className="empty-state">No files were changed by this step.</p>
            )}

            {review.files.length > 0 && (
                <ul className="step-review-files">
                    {review.files.map((f) => (
                        <li key={f.path} className={`step-review-file state-${f.state.toLowerCase()}`}>
                            <code className="step-review-path">{f.path}</code>
                            <span className="step-review-state">{f.state}</span>
                            <div className="step-review-actions">
                                <button
                                    type="button"
                                    className="btn btn-ghost"
                                    onClick={() => onOpenSub?.('diff', { path: f.path })}
                                >
                                    View diff
                                </button>
                                {f.state === 'Pending' && (
                                    <>
                                        <button
                                            type="button"
                                            className="btn btn-primary"
                                            onClick={() => commit(f.path)}
                                            disabled={busyPath === f.path}
                                        >
                                            {busyPath === f.path ? 'Committing…' : 'Commit'}
                                        </button>
                                        <button
                                            type="button"
                                            className="btn btn-ghost btn-danger"
                                            onClick={() => rollback(f.path)}
                                            disabled={busyPath === f.path}
                                        >
                                            Roll back
                                        </button>
                                    </>
                                )}
                                {f.state !== 'Pending' && f.resolvedAt && (
                                    <span className="step-review-resolved">
                                        {new Date(f.resolvedAt).toLocaleString()}
                                    </span>
                                )}
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
