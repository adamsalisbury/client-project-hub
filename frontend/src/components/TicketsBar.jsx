export default function TicketsBar({ tickets, onAdd, onSelect }) {
    return (
        <div className="tickets-bar">
            <div className="tickets-bar-header">
                <span className="tickets-bar-label">Tickets</span>
                <button
                    type="button"
                    className="tickets-bar-add"
                    onClick={onAdd}
                    title="Add a ticket"
                    aria-label="Add a ticket"
                >
                    <span aria-hidden="true">＋</span>
                    <span>Add</span>
                </button>
            </div>
            <div className="tickets-bar-list">
                {tickets.length === 0 && (
                    <span className="tickets-bar-empty">No tickets yet.</span>
                )}
                {tickets.map((t) => (
                    <button
                        key={t.id}
                        type="button"
                        className="ticket-chip"
                        onClick={() => onSelect?.(t)}
                        title={t.title}
                    >
                        <span className="ticket-chip-code">{t.code}</span>
                        <span className="ticket-chip-title">{t.title}</span>
                    </button>
                ))}
            </div>
        </div>
    );
}
