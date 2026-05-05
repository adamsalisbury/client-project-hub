import ChatHistory from './ChatHistory.jsx';
import MessageInput from './MessageInput.jsx';

export default function ChatTab({ project, history, loading, submitting, onSubmit, onViewFullMessage }) {
    return (
        <div className="chat-pane">
            <ChatHistory
                project={project}
                messages={history?.messages ?? []}
                loading={loading && !history}
                onViewFullMessage={onViewFullMessage}
            />
            <MessageInput
                onSubmit={onSubmit}
                submitting={submitting}
                disabled={false}
            />
        </div>
    );
}
