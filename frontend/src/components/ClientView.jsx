import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';
import ColourPicker from './ColourPicker.jsx';
import PathBrowser from './PathBrowser.jsx';

/**
 * The "client tab" body. Shows the client's projects, registered repos,
 * client knowledge, and a colour picker for the tab tint that propagates to
 * every nested project / sub tab.
 */
export default function ClientView({
    clientId,
    client,
    projects,
    onClientChanged,
    onProjectsChanged,
    onOpenProject,
    onNewProject,
    onError
}) {
    const [repos, setRepos] = useState([]);
    const [knowledge, setKnowledge] = useState([]);
    const [showRepoForm, setShowRepoForm] = useState(false);
    const [repoName, setRepoName] = useState('');
    const [repoPath, setRepoPath] = useState('');
    const [repoBrowserOpen, setRepoBrowserOpen] = useState(false);
    const [savingRepo, setSavingRepo] = useState(false);

    const loadRepos = useCallback(async () => {
        if (!clientId) return;
        try {
            setRepos((await api.listClientRepos(clientId)) ?? []);
        } catch (err) {
            onError?.(err.message);
        }
    }, [clientId, onError]);

    const loadKnowledge = useCallback(async () => {
        if (!clientId) return;
        try {
            setKnowledge((await api.listClientKnowledge(clientId)) ?? []);
        } catch (err) {
            onError?.(err.message);
        }
    }, [clientId, onError]);

    useEffect(() => { loadRepos(); loadKnowledge(); }, [loadRepos, loadKnowledge]);

    const handleColourChange = async (colour) => {
        try {
            const updated = await api.setClientColour(clientId, colour);
            onClientChanged?.(updated);
        } catch (err) {
            onError?.(err.message);
        }
    };

    const handleAddRepo = async (e) => {
        e?.preventDefault?.();
        if (!repoName.trim() || !repoPath.trim()) return;
        setSavingRepo(true);
        try {
            await api.addClientRepo(clientId, repoName.trim(), repoPath.trim());
            await loadRepos();
            setShowRepoForm(false);
            setRepoName('');
            setRepoPath('');
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSavingRepo(false);
        }
    };

    const handleRemoveRepo = async (repoId) => {
        if (!window.confirm('Remove this repo? Projects pointing at it will be detached.')) return;
        try {
            await api.removeClientRepo(clientId, repoId);
            await loadRepos();
            await onProjectsChanged?.();
        } catch (err) {
            onError?.(err.message);
        }
    };

    if (!client) {
        return <div className="workspace-empty">Loading client…</div>;
    }

    return (
        <div className="client-view">
            <header className="client-view-header" style={{ '--client-colour': client.colour }}>
                <div className="client-view-identity">
                    <h1 className="client-view-name">{client.name}</h1>
                    <span className="client-view-id">{client.id.slice(0, 8)}</span>
                </div>
                <div className="client-view-colour">
                    <span className="field-label">Tab colour</span>
                    <ColourPicker value={client.colour} onChange={handleColourChange} />
                </div>
            </header>

            <section className="client-view-section">
                <header className="client-view-section-head">
                    <h2>Projects</h2>
                    <button type="button" className="btn btn-primary" onClick={onNewProject}>＋ New project</button>
                </header>
                {projects.length === 0 && <p className="empty-state subtle">No projects under this client yet.</p>}
                {projects.length > 0 && (
                    <ul className="client-project-list">
                        {projects.map((p) => (
                            <li key={p.id}>
                                <button
                                    type="button"
                                    className="client-project-item"
                                    onClick={() => onOpenProject(p.id)}
                                    title={p.workingDirectory}
                                >
                                    <span className="client-project-name">{p.name}</span>
                                    <code className="client-project-cwd">{p.workingDirectory}</code>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            <section className="client-view-section">
                <header className="client-view-section-head">
                    <h2>Registered repos</h2>
                    <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setShowRepoForm((v) => !v)}
                    >
                        {showRepoForm ? 'Cancel' : '＋ Register repo'}
                    </button>
                </header>

                {showRepoForm && (
                    <form className="repo-form" onSubmit={handleAddRepo}>
                        <label className="field-label" htmlFor="repo-name">Name</label>
                        <input
                            id="repo-name"
                            type="text"
                            className="text-input"
                            placeholder="e.g. core-api"
                            value={repoName}
                            onChange={(e) => setRepoName(e.target.value)}
                            disabled={savingRepo}
                            maxLength={120}
                        />
                        <label className="field-label">Path</label>
                        <div className="working-dir-row">
                            <code className="working-dir-display">
                                {repoPath || <span className="placeholder">No folder selected</span>}
                            </code>
                            <button
                                type="button"
                                className="btn btn-ghost"
                                onClick={() => setRepoBrowserOpen((v) => !v)}
                                disabled={savingRepo}
                            >
                                {repoBrowserOpen ? 'Hide browser' : 'Browse…'}
                            </button>
                        </div>
                        {repoBrowserOpen && (
                            <PathBrowser
                                initialPath={repoPath}
                                onSelect={(path) => { setRepoPath(path); setRepoBrowserOpen(false); }}
                                onCancel={() => setRepoBrowserOpen(false)}
                            />
                        )}
                        <div className="modal-actions">
                            <button
                                type="submit"
                                className="btn btn-primary"
                                disabled={savingRepo || !repoName.trim() || !repoPath.trim()}
                            >
                                {savingRepo ? 'Registering…' : 'Register'}
                            </button>
                        </div>
                    </form>
                )}

                {repos.length === 0 && !showRepoForm && (
                    <p className="empty-state subtle">No repos registered yet.</p>
                )}
                {repos.length > 0 && (
                    <ul className="repo-list">
                        {repos.map((r) => (
                            <li key={r.id} className="repo-list-item">
                                <div className="repo-list-name">{r.name}</div>
                                <code className="repo-list-path">{r.path}</code>
                                <button
                                    type="button"
                                    className="btn btn-ghost btn-danger"
                                    onClick={() => handleRemoveRepo(r.id)}
                                >Remove</button>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            <section className="client-view-section">
                <header className="client-view-section-head">
                    <h2>Client knowledge</h2>
                </header>
                {knowledge.length === 0 && <p className="empty-state subtle">No knowledge entries yet.</p>}
                {knowledge.length > 0 && (
                    <ul className="knowledge-list">
                        {knowledge.map((k) => (
                            <li key={k.id} className="knowledge-list-item">
                                <span className="knowledge-list-title">{k.title}</span>
                            </li>
                        ))}
                    </ul>
                )}
            </section>
        </div>
    );
}
