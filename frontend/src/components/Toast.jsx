import { useEffect } from 'react';

export default function Toast({ message, onDismiss }) {
    useEffect(() => {
        if (!message) return;
        const id = setTimeout(onDismiss, 6000);
        return () => clearTimeout(id);
    }, [message, onDismiss]);

    if (!message) return null;

    return (
        <div className="toast" role="alert" onClick={onDismiss}>
            <span>{message}</span>
            <button type="button" className="toast-close" aria-label="Dismiss">×</button>
        </div>
    );
}
