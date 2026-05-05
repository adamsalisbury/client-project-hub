const ICONS = {
    info: 'ℹ',
    files: '📁',
    chat: '💬',
    file: '📄',
    diff: '⇄',
    ticket: '🎫',
    knowledge: '📘',
    agents: '🤖',
    agent: '🤖',
    'memory-tweak': '🧠'
};

export default function ProjectTabBar({ tabs, activeId, onActivate, onClose }) {
    return (
        <div className="proj-tabbar" role="tablist" aria-label="Project tabs">
            {tabs.map((tab) => {
                const active = tab.id === activeId;
                return (
                    <div
                        key={tab.id}
                        role="tab"
                        aria-selected={active}
                        className={`proj-tab ${active ? 'active' : ''}`}
                        title={tab.tooltip || tab.label}
                    >
                        <button
                            type="button"
                            className="proj-tab-label"
                            onClick={() => onActivate(tab.id)}
                        >
                            <span className="proj-tab-icon" aria-hidden="true">{ICONS[tab.kind] ?? '·'}</span>
                            <span className="proj-tab-text">{tab.label}</span>
                        </button>
                        {tab.closable && (
                            <button
                                type="button"
                                className="proj-tab-close"
                                onClick={() => onClose(tab.id)}
                                aria-label={`Close ${tab.label}`}
                                title="Close"
                            >×</button>
                        )}
                    </div>
                );
            })}
        </div>
    );
}
