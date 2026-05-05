import { useMemo } from 'react';
import { Marked } from 'marked';
import DOMPurify from 'dompurify';
import { highlight as highlightCode, escapeHtml } from '../highlighter.js';

function buildMarked() {
    const m = new Marked({ gfm: true, breaks: false });
    m.use({
        renderer: {
            code({ text, lang }) {
                const language = (lang || '').trim().toLowerCase();
                const highlighted = language
                    ? highlightCode(text, language)
                    : escapeHtml(text);
                const cls = language ? `hljs language-${language}` : 'hljs';
                return `<pre class="md-pre"><code class="${cls}">${highlighted}</code></pre>`;
            }
        }
    });
    return m;
}

const marked = buildMarked();

export default function MarkdownView({ source }) {
    const html = useMemo(() => {
        if (!source) return '';
        const raw = marked.parse(source);
        return DOMPurify.sanitize(raw, {
            ADD_ATTR: ['target', 'rel']
        });
    }, [source]);

    if (!source) {
        return <div className="empty-state subtle">Nothing to show.</div>;
    }

    return <div className="markdown-view" dangerouslySetInnerHTML={{ __html: html }} />;
}
