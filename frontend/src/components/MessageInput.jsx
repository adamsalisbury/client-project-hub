import { useCallback, useState } from 'react';

export default function MessageInput({ onSubmit, submitting, disabled }) {
    const [value, setValue] = useState('');

    const submit = useCallback(async () => {
        const trimmed = value.trim();
        if (!trimmed || submitting || disabled) return;
        await onSubmit(trimmed, 'Chat');
        setValue('');
    }, [value, submitting, disabled, onSubmit]);

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
            e.preventDefault();
            submit();
        }
    };

    return (
        <form className="composer" onSubmit={(e) => { e.preventDefault(); submit(); }}>
            <textarea
                className="composer-textarea"
                placeholder="Write message"
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
                {submitting ? 'Sending…' : 'Send'}
            </button>
        </form>
    );
}
