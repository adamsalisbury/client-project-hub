/**
 * Top-level tab strip. Renders the Home tab (always pinned, unclosable)
 * followed by every open client / project / sub tab. Each non-home tab is
 * coloured by the owning client's hex colour so the user can see at a glance
 * which clients and projects are in play.
 */
export default function WorkspaceTabBar({ tabs, activeId, onActivate, onClose }) {
    return (
        <div className="ws-tabs" role="tablist">
            {tabs.map((tab) => {
                const isActive = tab.id === activeId;
                const style = tab.colour
                    ? { '--tab-strip-colour': tab.colour }
                    : undefined;
                return (
                    <div
                        key={tab.id}
                        role="tab"
                        aria-selected={isActive}
                        tabIndex={0}
                        className={`ws-tab ws-tab-${tab.kind} ${isActive ? 'active' : ''} ${tab.colour ? 'has-colour' : ''}`}
                        style={style}
                        onClick={() => onActivate(tab.id)}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter' || e.key === ' ') {
                                e.preventDefault();
                                onActivate(tab.id);
                            }
                        }}
                        title={tab.tooltip || tab.label}
                    >
                        <span className="ws-tab-label">{tab.label}</span>
                        {tab.closable && (
                            <button
                                type="button"
                                className="ws-tab-close"
                                onClick={(e) => { e.stopPropagation(); onClose(tab.id); }}
                                aria-label={`Close ${tab.label}`}
                            >×</button>
                        )}
                    </div>
                );
            })}
        </div>
    );
}
