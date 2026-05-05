import { useEffect, useState } from 'react';
import { api } from '../api.js';
import MarkdownView from './MarkdownView.jsx';

function formatTimestamp(iso) {
    if (!iso) return '';
    try {
        return new Date(iso).toLocaleString();
    } catch {
        return iso;
    }
}

export default function TicketTab({ project, ticket: providedTicket, ticketId, tickets, onError }) {
    const [ticket, setTicket] = useState(providedTicket ?? null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (providedTicket) { setTicket(providedTicket); return; }
        if (!ticketId) return;
        const fromList = tickets?.find((t) => t.id === ticketId);
        if (fromList) { setTicket(fromList); return; }
        let cancelled = false;
        setLoading(true);
        api.listTickets(project.id)
            .then((list) => {
                if (cancelled) return;
                const found = (list ?? []).find((t) => t.id === ticketId);
                if (!found) {
                    onError?.(`Ticket ${ticketId} not found in project.`);
                } else {
                    setTicket(found);
                }
            })
            .catch((err) => onError?.(err.message))
            .finally(() => { if (!cancelled) setLoading(false); });
        return () => { cancelled = true; };
    }, [providedTicket, ticketId, tickets, project.id, onError]);

    if (!ticket) {
        return <div className="empty-state subtle">{loading ? 'Loading.' : 'Ticket not available.'}</div>;
    }

    return (
        <div className="document-tab">
            <header className="document-tab-header">
                <div className="document-tab-meta">
                    <span className="document-tab-eyebrow">Ticket</span>
                    <code className="document-tab-code">{ticket.code}</code>
                    <span className="document-tab-date">created {formatTimestamp(ticket.createdAt)}</span>
                </div>
                <h1 className="document-tab-title">{ticket.title}</h1>
            </header>
            <div className="document-tab-body">
                <MarkdownView source={ticket.body} />
            </div>
        </div>
    );
}
