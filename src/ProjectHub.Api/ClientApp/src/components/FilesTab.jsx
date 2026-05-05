import { useCallback, useEffect, useRef, useState } from 'react';
import FileTree from './FileTree.jsx';
import FileViewer from './FileViewer.jsx';
import ContextMenu from './ContextMenu.jsx';

const MIN_LEFT = 220;
const MIN_RIGHT = 320;
const STORAGE_KEY = 'project-hub-file-split';

export default function FilesTab({ project, openFileTab, openDiffTab, onError }) {
    const [currentPath, setCurrentPath] = useState('');
    const [selectedFile, setSelectedFile] = useState(null);
    const [contextMenu, setContextMenu] = useState(null);
    const [leftWidth, setLeftWidth] = useState(() => {
        const stored = Number(localStorage.getItem(STORAGE_KEY));
        return Number.isFinite(stored) && stored >= MIN_LEFT ? stored : 320;
    });
    const containerRef = useRef(null);
    const draggingRef = useRef(false);

    useEffect(() => {
        setCurrentPath('');
        setSelectedFile(null);
    }, [project?.id]);

    const handleSelectFile = useCallback((path) => setSelectedFile(path), []);
    const handleNavigate = useCallback((path) => setCurrentPath(path || ''), []);

    const handleContextMenu = useCallback((event, entry) => {
        if (entry.isDirectory) return;
        event.preventDefault();
        setContextMenu({
            x: event.clientX,
            y: event.clientY,
            path: entry.relativePath,
            name: entry.name
        });
    }, []);

    const closeMenu = () => setContextMenu(null);

    useEffect(() => {
        const onMove = (e) => {
            if (!draggingRef.current || !containerRef.current) return;
            const rect = containerRef.current.getBoundingClientRect();
            const x = (e.touches ? e.touches[0].clientX : e.clientX) - rect.left;
            const max = rect.width - MIN_RIGHT;
            const clamped = Math.max(MIN_LEFT, Math.min(x, max));
            setLeftWidth(clamped);
        };
        const onUp = () => {
            if (draggingRef.current) {
                draggingRef.current = false;
                document.body.classList.remove('dragging-splitter');
                localStorage.setItem(STORAGE_KEY, String(leftWidth));
            }
        };

        window.addEventListener('mousemove', onMove);
        window.addEventListener('mouseup', onUp);
        window.addEventListener('touchmove', onMove);
        window.addEventListener('touchend', onUp);
        return () => {
            window.removeEventListener('mousemove', onMove);
            window.removeEventListener('mouseup', onUp);
            window.removeEventListener('touchmove', onMove);
            window.removeEventListener('touchend', onUp);
        };
    }, [leftWidth]);

    const startDrag = (e) => {
        e.preventDefault();
        draggingRef.current = true;
        document.body.classList.add('dragging-splitter');
    };

    if (!project) {
        return (
            <div className="file-browser-empty">
                <p>No project selected.</p>
            </div>
        );
    }

    return (
        <div
            className="file-browser"
            ref={containerRef}
            style={{ gridTemplateColumns: `${leftWidth}px 4px 1fr` }}
        >
            <div className="file-browser-pane left">
                <FileTree
                    projectId={project.id}
                    currentPath={currentPath}
                    onNavigate={handleNavigate}
                    onSelectFile={handleSelectFile}
                    onContextMenu={handleContextMenu}
                    selectedFile={selectedFile}
                />
            </div>
            <div
                className="splitter"
                role="separator"
                aria-orientation="vertical"
                onMouseDown={startDrag}
                onTouchStart={startDrag}
            />
            <div className="file-browser-pane right">
                <FileViewer projectId={project.id} path={selectedFile} />
            </div>

            {contextMenu && (
                <ContextMenu
                    x={contextMenu.x}
                    y={contextMenu.y}
                    items={[
                        {
                            icon: '📄',
                            label: `Open '${contextMenu.name}' in new tab`,
                            onClick: () => openFileTab(contextMenu.path)
                        },
                        {
                            icon: '⇄',
                            label: 'Show local changes',
                            onClick: () => openDiffTab(contextMenu.path)
                        }
                    ]}
                    onClose={closeMenu}
                />
            )}
        </div>
    );
}
