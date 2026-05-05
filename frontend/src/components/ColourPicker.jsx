import { useEffect, useRef, useState } from 'react';

/**
 * 16 light hex colours used to tint client tabs. Mirrors the server-side
 * palette in ProjectHub.Domain.Models.ClientColours so the picker offers
 * the same set of options the server picks from.
 */
export const CLIENT_COLOUR_PALETTE = [
    '#FED7D7', '#FEEBC8', '#FEFCBF', '#C6F6D5',
    '#B2F5EA', '#BEE3F8', '#C3DAFE', '#D6BCFA',
    '#FBB6CE', '#FAD2E1', '#F6E0B5', '#D9F99D',
    '#A7F3D0', '#A5F3FC', '#BAE6FD', '#E9D5FF'
];

export default function ColourPicker({ value, onChange, disabled }) {
    const [open, setOpen] = useState(false);
    const ref = useRef(null);

    useEffect(() => {
        if (!open) return undefined;
        const onDoc = (e) => {
            if (ref.current && !ref.current.contains(e.target)) {
                setOpen(false);
            }
        };
        document.addEventListener('mousedown', onDoc);
        return () => document.removeEventListener('mousedown', onDoc);
    }, [open]);

    return (
        <div className="colour-picker" ref={ref}>
            <button
                type="button"
                className="colour-picker-swatch"
                style={{ background: value || '#E2E8F0' }}
                onClick={() => !disabled && setOpen((v) => !v)}
                aria-label="Pick colour"
                disabled={disabled}
            />
            {open && (
                <div className="colour-picker-popover" role="listbox">
                    {CLIENT_COLOUR_PALETTE.map((hex) => (
                        <button
                            key={hex}
                            type="button"
                            role="option"
                            aria-selected={hex === value}
                            className={`colour-picker-option ${hex === value ? 'selected' : ''}`}
                            style={{ background: hex }}
                            onClick={() => { onChange(hex); setOpen(false); }}
                            title={hex}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
