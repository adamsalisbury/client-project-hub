import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';

/**
 * Sub-tab body for the project's plan. Steps are linearly ordered: each step
 * assumes the prior one has been executed. Verify produces an opinion that
 * lands in the project chat; per-step Execute kicks off a Claude job.
 */
export default function PlanTab({ project, onOpenSub, onError }) {
    const [plan, setPlan] = useState(null);
    const [steps, setSteps] = useState([]);
    const [loading, setLoading] = useState(true);
    const [newTitle, setNewTitle] = useState('');
    const [newDesc, setNewDesc] = useState('');
    const [adding, setAdding] = useState(false);
    const [editingId, setEditingId] = useState(null);
    const [editTitle, setEditTitle] = useState('');
    const [editDesc, setEditDesc] = useState('');
    const [busy, setBusy] = useState(false);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await api.getPlan(project.id);
            setPlan(data);
            setSteps(data?.steps ?? []);
        } catch (err) {
            onError?.(err.message);
        } finally {
            setLoading(false);
        }
    }, [project.id, onError]);

    useEffect(() => { load(); }, [load]);

    const addStep = async (e) => {
        e?.preventDefault?.();
        if (!newTitle.trim()) return;
        setAdding(true);
        try {
            await api.addPlanStep(project.id, { title: newTitle.trim(), description: newDesc });
            setNewTitle('');
            setNewDesc('');
            await load();
        } catch (err) {
            onError?.(err.message);
        } finally {
            setAdding(false);
        }
    };

    const startEdit = (step) => {
        setEditingId(step.id);
        setEditTitle(step.title);
        setEditDesc(step.description ?? '');
    };

    const saveEdit = async () => {
        if (!editingId || !editTitle.trim()) return;
        setBusy(true);
        try {
            await api.updatePlanStep(project.id, editingId, {
                title: editTitle.trim(),
                description: editDesc
            });
            setEditingId(null);
            await load();
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusy(false);
        }
    };

    const remove = async (stepId) => {
        if (!window.confirm('Remove this step?')) return;
        setBusy(true);
        try {
            await api.deletePlanStep(project.id, stepId);
            await load();
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusy(false);
        }
    };

    const move = async (index, delta) => {
        const target = index + delta;
        if (target < 0 || target >= steps.length) return;
        const reordered = [...steps];
        const [moved] = reordered.splice(index, 1);
        reordered.splice(target, 0, moved);
        setSteps(reordered);
        try {
            await api.reorderPlanSteps(project.id, reordered.map((s) => s.id));
        } catch (err) {
            onError?.(err.message);
            await load();
        }
    };

    const verify = async () => {
        setBusy(true);
        try {
            await api.verifyPlan(project.id);
            onOpenSub?.('chat', {});
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusy(false);
        }
    };

    const execute = async (stepId) => {
        setBusy(true);
        try {
            await api.executePlanStep(project.id, stepId);
            onOpenSub?.('chat', {});
        } catch (err) {
            onError?.(err.message);
        } finally {
            setBusy(false);
        }
    };

    if (loading) return <div className="workspace-empty">Loading plan…</div>;

    return (
        <div className="plan-tab">
            <header className="plan-tab-head">
                <h1>Plan</h1>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={verify}
                    disabled={busy || steps.length === 0}
                    title={steps.length === 0 ? 'Add a step before verifying' : 'Ask Claude to review the plan'}
                >
                    Verify plan
                </button>
            </header>

            {plan?.lastVerificationOpinion && (
                <section className="plan-verification">
                    <div className="plan-verification-head">
                        Last verification — {new Date(plan.lastVerifiedAt).toLocaleString()}
                    </div>
                    <pre className="plan-verification-body">{plan.lastVerificationOpinion}</pre>
                </section>
            )}

            <ol className="plan-steps">
                {steps.map((s, i) => (
                    <li key={s.id} className={`plan-step status-${s.status?.toLowerCase()}`}>
                        <div className="plan-step-head">
                            <span className="plan-step-number">{i + 1}.</span>
                            {editingId === s.id ? (
                                <input
                                    type="text"
                                    className="text-input"
                                    value={editTitle}
                                    onChange={(e) => setEditTitle(e.target.value)}
                                    disabled={busy}
                                />
                            ) : (
                                <span className="plan-step-title">{s.title}</span>
                            )}
                            <span className="plan-step-status">{s.status}</span>
                        </div>
                        {editingId === s.id ? (
                            <textarea
                                className="text-input"
                                rows={4}
                                value={editDesc}
                                onChange={(e) => setEditDesc(e.target.value)}
                                disabled={busy}
                            />
                        ) : (
                            s.description && <pre className="plan-step-description">{s.description}</pre>
                        )}
                        <div className="plan-step-actions">
                            {editingId === s.id ? (
                                <>
                                    <button type="button" className="btn btn-primary" onClick={saveEdit} disabled={busy || !editTitle.trim()}>
                                        Save
                                    </button>
                                    <button type="button" className="btn btn-ghost" onClick={() => setEditingId(null)} disabled={busy}>
                                        Cancel
                                    </button>
                                </>
                            ) : (
                                <>
                                    <button type="button" className="btn btn-ghost" onClick={() => move(i, -1)} disabled={busy || i === 0}>↑</button>
                                    <button type="button" className="btn btn-ghost" onClick={() => move(i, 1)} disabled={busy || i === steps.length - 1}>↓</button>
                                    <button type="button" className="btn btn-ghost" onClick={() => startEdit(s)} disabled={busy}>Edit</button>
                                    <button type="button" className="btn btn-ghost btn-danger" onClick={() => remove(s.id)} disabled={busy}>Delete</button>
                                    <button
                                        type="button"
                                        className="btn btn-primary"
                                        onClick={() => execute(s.id)}
                                        disabled={busy || s.status === 'Running'}
                                    >
                                        {s.status === 'Running' ? 'Running…' : 'Execute'}
                                    </button>
                                    {s.jobId && s.status !== 'Pending' && (
                                        <button
                                            type="button"
                                            className="btn btn-ghost"
                                            onClick={() => onOpenSub?.('diagnostics', { jobId: s.jobId })}
                                        >
                                            Diagnostics
                                        </button>
                                    )}
                                </>
                            )}
                        </div>
                    </li>
                ))}
            </ol>

            <form className="plan-add-step" onSubmit={addStep}>
                <h3>Add step</h3>
                <label className="field-label" htmlFor="plan-step-title">Title</label>
                <input
                    id="plan-step-title"
                    type="text"
                    className="text-input"
                    placeholder="e.g. Wire up the API endpoint"
                    value={newTitle}
                    onChange={(e) => setNewTitle(e.target.value)}
                    disabled={adding}
                    maxLength={200}
                />
                <label className="field-label" htmlFor="plan-step-desc">Description</label>
                <textarea
                    id="plan-step-desc"
                    className="text-input"
                    rows={4}
                    placeholder="Detail what this step requires…"
                    value={newDesc}
                    onChange={(e) => setNewDesc(e.target.value)}
                    disabled={adding}
                />
                <div className="modal-actions">
                    <button type="submit" className="btn btn-primary" disabled={adding || !newTitle.trim()}>
                        {adding ? 'Adding…' : 'Add step'}
                    </button>
                </div>
            </form>
        </div>
    );
}
