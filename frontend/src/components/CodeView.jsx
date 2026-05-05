import { useMemo } from 'react';
import { highlight, languageForPath } from '../highlighter.js';

export default function CodeView({ code, language, path, showLineNumbers = true }) {
    const lang = language || languageForPath(path);
    const html = useMemo(() => highlight(code || '', lang), [code, lang]);
    const lines = useMemo(() => (code || '').split('\n'), [code]);

    if (!code) {
        return <div className="empty-state subtle">Empty file.</div>;
    }

    return (
        <div className={`code-view ${showLineNumbers ? 'with-gutter' : ''}`} data-language={lang}>
            {showLineNumbers && (
                <div className="code-gutter" aria-hidden="true">
                    {lines.map((_, i) => (
                        <span key={i} className="code-gutter-line">{i + 1}</span>
                    ))}
                </div>
            )}
            <pre className="code-pre"><code className={`hljs language-${lang}`} dangerouslySetInnerHTML={{ __html: html }} /></pre>
        </div>
    );
}
