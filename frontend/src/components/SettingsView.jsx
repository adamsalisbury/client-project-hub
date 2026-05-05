import { useEffect, useState } from 'react';
import { api } from '../api.js';

/**
 * Body of the Settings tab. Lets the user edit application-wide settings
 * such as the name to address the AI by.
 */
export default function SettingsView({ onError }) {
    const [aiName, setAiName] = useState('');
    const [original, setOriginal] = useState('');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [savedAt, setSavedAt] = useState(null);

    useEffect(() => {
        let cancelled = false;
        async function load() {
            try {
                const settings = await api.getSettings();
                if (!cancelled) {
                    const value = settings?.aiName ?? '';
                    setAiName(value);
                    setOriginal(value);
                }
            } catch (err) {
                if (!cancelled) onError?.(err.message);
            } finally {
                if (!cancelled) setLoading(false);
            }
        }
        load();
        return () => { cancelled = true; };
    }, [onError]);

    const save = async () => {
        setSaving(true);
        try {
            const trimmed = aiName.trim();
            const saved = await api.updateSettings({ aiName: trimmed.length === 0 ? null : trimmed });
            const value = saved?.aiName ?? '';
            setAiName(value);
            setOriginal(value);
            setSavedAt(new Date());
        } catch (err) {
            onError?.(err.message);
        } finally {
            setSaving(false);
        }
    };

    const dirty = aiName !== original;

    return (
        <div className="settings-view">
            <header className="page-title-row">
                <h1 className="page-title">Settings</h1>
            </header>

            <section className="info-section">
                <div className="info-section-header">
                    <h2 className="info-section-title">AI</h2>
                </div>
                <p className="info-section-help">
                    Pick a name to address the AI by. The name is included in
                    the chat prompt so the AI introduces itself as that name.
                </p>
                <label className="field-label" htmlFor="settings-ai-name">AI name</label>
                <input
                    id="settings-ai-name"
                    type="text"
                    className="text-input"
                    placeholder="e.g. Atlas"
                    value={aiName}
                    onChange={(e) => setAiName(e.target.value)}
                    disabled={loading || saving}
                    maxLength={80}
                />
                <div className="modal-actions">
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={save}
                        disabled={saving || loading || !dirty}
                    >
                        {saving ? 'Saving…' : 'Save'}
                    </button>
                    {savedAt && !dirty && (
                        <span className="settings-saved-marker">Saved {savedAt.toLocaleTimeString()}</span>
                    )}
                </div>
            </section>
        </div>
    );
}
