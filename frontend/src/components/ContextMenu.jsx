import { useEffect } from 'react';

export default function ContextMenu({ x, y, items, onClose }) {
    useEffect(() => {
        const handleAway = () => onClose();
        const handleKey = (e) => { if (e.key === 'Escape') onClose(); };
        window.addEventListener('mousedown', handleAway);
        window.addEventListener('scroll', handleAway, true);
        window.addEventListener('keydown', handleKey);
        return () => {
            window.removeEventListener('mousedown', handleAway);
            window.removeEventListener('scroll', handleAway, true);
            window.removeEventListener('keydown', handleKey);
        };
    }, [onClose]);

    return (
        <ul
            className="context-menu"
            style={{ left: x, top: y }}
            role="menu"
            onMouseDown={(e) => e.stopPropagation()}
        >
            {items.map((item, idx) => (
                <li key={idx} role="menuitem">
                    <button
                        type="button"
                        className="context-menu-item"
                        onClick={() => { item.onClick(); onClose(); }}
                        disabled={item.disabled}
                    >
                        {item.icon && <span aria-hidden="true">{item.icon}</span>}
                        <span>{item.label}</span>
                    </button>
                </li>
            ))}
        </ul>
    );
}
