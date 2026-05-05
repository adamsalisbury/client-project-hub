import { useCallback, useState } from 'react';

export default function MessageInput({ onSubmit, submitting, disabled }) {
    const [value, setValue] = useState('');
    const [kind, setKind] = useState('Chat');

    const submit = useCallback(async () => {
        const trimmed = value.trim();
        if (!trimmed || submitting || disabled) return;
        await onSubmit(trimmed, kind);
        setValue('');
    }, [value, kind, submitting, disabled, onSubmit]);

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
            e.preventDefault();
            submit();
        }
    };

    const placeholder = kind === 'Edit'
        ? 'Describe the change to make - Claude will edit files. ⌘/Ctrl+Enter to submit'
        : 'Send a message - Claude can read but not edit. ⌘/Ctrl+Enter to submit';

    return (
        <form className="composer" onSubmit={(e) => { e.preventDefault(); submit(); }}>
            <div className="kind-toggle" role="radiogroup" aria-label="Message kind">
                <button
                    type="button"
                    role="radio"
                    aria-checked={kind === 'Chat'}
                    className={`kind-option ${kind === 'Chat' ? 'active' : ''}`}
                    onClick={() => setKind('Chat')}
                    disabled={disabled || submitting}
                    title="Read-only - Claude cannot change files"
                >
                    <span aria-hidden="true">💬</span>
                    <span>Chat</span>
                </button>
                <button
                    type="button"
                    role="radio"
                    aria-checked={kind === 'Edit'}
                    className={`kind-option ${kind === 'Edit' ? 'active' : ''}`}
                    onClick={() => setKind('Edit')}
                    disabled={disabled || submitting}
                    title="Allow Claude to edit files in the working directory"
                >
                    <span aria-hidden="true">✎</span>
                    <span>Edit</span>
                </button>
            </div>

            <textarea
                className="composer-textarea"
                placeholder={placeholder}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={disabled || submitting}
                rows={6}
            />
            <button
                type="submit"
                className="btn btn-primary composer-submit"
                disabled={disabled || submitting || !value.trim()}
            >
                {submitting ? 'Sending…' : (kind === 'Edit' ? 'Run edit' : 'Send')}
            </button>
        </form>
    );
}
