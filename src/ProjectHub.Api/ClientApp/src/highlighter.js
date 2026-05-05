import hljs from 'highlight.js/lib/core';

import csharp from 'highlight.js/lib/languages/csharp';
import javascript from 'highlight.js/lib/languages/javascript';
import typescript from 'highlight.js/lib/languages/typescript';
import yaml from 'highlight.js/lib/languages/yaml';
import xml from 'highlight.js/lib/languages/xml';
import json from 'highlight.js/lib/languages/json';
import markdown from 'highlight.js/lib/languages/markdown';
import diff from 'highlight.js/lib/languages/diff';
import bash from 'highlight.js/lib/languages/bash';
import css from 'highlight.js/lib/languages/css';
import plaintext from 'highlight.js/lib/languages/plaintext';

hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('cs', csharp);
hljs.registerLanguage('javascript', javascript);
hljs.registerLanguage('js', javascript);
hljs.registerLanguage('jsx', javascript);
hljs.registerLanguage('typescript', typescript);
hljs.registerLanguage('ts', typescript);
hljs.registerLanguage('tsx', typescript);
hljs.registerLanguage('yaml', yaml);
hljs.registerLanguage('yml', yaml);
hljs.registerLanguage('xml', xml);
hljs.registerLanguage('html', xml);
hljs.registerLanguage('svg', xml);
hljs.registerLanguage('json', json);
hljs.registerLanguage('markdown', markdown);
hljs.registerLanguage('md', markdown);
hljs.registerLanguage('diff', diff);
hljs.registerLanguage('patch', diff);
hljs.registerLanguage('bash', bash);
hljs.registerLanguage('sh', bash);
hljs.registerLanguage('shell', bash);
hljs.registerLanguage('css', css);
hljs.registerLanguage('plaintext', plaintext);
hljs.registerLanguage('text', plaintext);

const EXTENSION_TO_LANGUAGE = {
    cs: 'csharp',
    csx: 'csharp',
    js: 'javascript',
    mjs: 'javascript',
    cjs: 'javascript',
    jsx: 'javascript',
    ts: 'typescript',
    tsx: 'typescript',
    yaml: 'yaml',
    yml: 'yaml',
    html: 'xml',
    htm: 'xml',
    xml: 'xml',
    svg: 'xml',
    json: 'json',
    md: 'markdown',
    markdown: 'markdown',
    css: 'css',
    sh: 'bash',
    bash: 'bash',
    diff: 'diff',
    patch: 'diff'
};

export function languageForPath(path) {
    if (!path) return 'plaintext';
    const lower = path.toLowerCase();
    const baseName = lower.split('/').pop();
    if (baseName === 'dockerfile') return 'plaintext';
    if (baseName === '.editorconfig') return 'plaintext';
    if (baseName === 'directory.build.props' || baseName.endsWith('.csproj') || baseName.endsWith('.props') || baseName.endsWith('.targets') || baseName.endsWith('.config')) {
        return 'xml';
    }
    const dot = baseName.lastIndexOf('.');
    if (dot < 0) return 'plaintext';
    const ext = baseName.slice(dot + 1);
    return EXTENSION_TO_LANGUAGE[ext] ?? 'plaintext';
}

export function highlight(code, language) {
    if (!code) return '';
    const lang = language && hljs.getLanguage(language) ? language : 'plaintext';
    try {
        return hljs.highlight(code, { language: lang, ignoreIllegals: true }).value;
    } catch {
        return escapeHtml(code);
    }
}

export function escapeHtml(text) {
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
