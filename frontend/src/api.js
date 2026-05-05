function readCookie(name) {
    const match = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/[.$?*|{}()[\]\\\/+^]/g, '\\$&') + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
}

function antiforgeryHeaders() {
    const token = readCookie('XSRF-TOKEN');
    return token ? { 'X-XSRF-TOKEN': token } : {};
}

async function request(method, url, body) {
    const headers = { Accept: 'application/json' };
    if (body !== undefined) {
        headers['Content-Type'] = 'application/json';
    }
    if (method !== 'GET') {
        Object.assign(headers, antiforgeryHeaders());
    }

    const response = await fetch(url, {
        method,
        headers,
        credentials: 'same-origin',
        body: body !== undefined ? JSON.stringify(body) : undefined
    });

    return handleResponse(response);
}

async function requestForm(method, url, formData) {
    const headers = { Accept: 'application/json', ...antiforgeryHeaders() };

    const response = await fetch(url, {
        method,
        headers,
        credentials: 'same-origin',
        body: formData
    });

    return handleResponse(response);
}

async function handleResponse(response) {
    if (response.status === 404) {
        return null;
    }
    if (!response.ok) {
        let message = `HTTP ${response.status}`;
        try {
            const data = await response.json();
            if (data?.error) message = data.error;
        } catch { /* ignore */ }
        throw new Error(message);
    }
    if (response.status === 204) {
        return null;
    }
    return response.json();
}

export const api = {
    listProjects: () => request('GET', '/api/projects'),
    createProject: (payload) => request('POST', '/api/projects', payload),
    updateProject: (id, payload) => request('PUT', `/api/projects/${id}`, payload),
    assignProjectRepo: (projectId, repoId) =>
        request('PUT', `/api/projects/${projectId}/repo`, { repoId }),
    projectHistory: (id) => request('GET', `/api/projects/${id}/history`),
    submit: (projectId, message, kind = 'Chat') => request('POST', '/api/claude', { projectId, message, kind }),
    jobStatus: (id) => request('GET', `/api/claude/${id}`),
    browseFilesystem: (path) => {
        const url = path
            ? `/api/filesystem/browse?path=${encodeURIComponent(path)}`
            : '/api/filesystem/browse';
        return request('GET', url);
    },
    listProjectFiles: (projectId, path) => {
        const url = path
            ? `/api/projects/${projectId}/files?path=${encodeURIComponent(path)}`
            : `/api/projects/${projectId}/files`;
        return request('GET', url);
    },
    readProjectFile: (projectId, path) =>
        request('GET', `/api/projects/${projectId}/file?path=${encodeURIComponent(path)}`),
    fileDiff: (projectId, path) =>
        request('GET', `/api/projects/${projectId}/file/diff?path=${encodeURIComponent(path)}`),
    fileHistory: (projectId, path) =>
        request('GET', `/api/projects/${projectId}/file/history?path=${encodeURIComponent(path)}`),
    listTickets: (projectId) =>
        request('GET', `/api/projects/${projectId}/tickets`),
    createTicket: (projectId, ticket) =>
        request('POST', `/api/projects/${projectId}/tickets`, ticket),
    extractTicketFromScreenshots: (projectId, files) => {
        const form = new FormData();
        for (const file of files) {
            form.append('files', file, file.name);
        }
        return requestForm('POST', `/api/projects/${projectId}/tickets/extract-from-screenshots`, form);
    },
    listKnowledge: (projectId) =>
        request('GET', `/api/projects/${projectId}/knowledge`),
    getKnowledge: (projectId, id) =>
        request('GET', `/api/projects/${projectId}/knowledge/${id}`),
    createKnowledge: (projectId, entry) =>
        request('POST', `/api/projects/${projectId}/knowledge`, entry),
    deleteKnowledge: (projectId, id) =>
        request('DELETE', `/api/projects/${projectId}/knowledge/${id}`),
    listClients: () =>
        request('GET', '/api/clients'),
    getClient: (id) =>
        request('GET', `/api/clients/${id}`),
    createClient: (name) =>
        request('POST', '/api/clients', { name }),
    listClientProjects: (clientId) =>
        request('GET', `/api/clients/${clientId}/projects`),
    listClientKnowledge: (clientId) =>
        request('GET', `/api/clients/${clientId}/knowledge`),
    getClientKnowledge: (clientId, id) =>
        request('GET', `/api/clients/${clientId}/knowledge/${id}`),
    createClientKnowledge: (clientId, entry) =>
        request('POST', `/api/clients/${clientId}/knowledge`, entry),
    deleteClientKnowledge: (clientId, id) =>
        request('DELETE', `/api/clients/${clientId}/knowledge/${id}`),
    assignClient: (projectId, clientId) =>
        request('PUT', `/api/projects/${projectId}/client`, { clientId }),
    memoryUsage: (projectId) =>
        request('GET', `/api/projects/${projectId}/memory-usage`),
    listAgents: (projectId) =>
        request('GET', `/api/projects/${projectId}/agents`),
    getAgent: (projectId, id) =>
        request('GET', `/api/projects/${projectId}/agents/${id}`),
    createAgent: (projectId, agent) =>
        request('POST', `/api/projects/${projectId}/agents`, agent),
    updateAgent: (projectId, id, agent) =>
        request('PUT', `/api/projects/${projectId}/agents/${id}`, agent),
    deleteAgent: (projectId, id) =>
        request('DELETE', `/api/projects/${projectId}/agents/${id}`),
    generateAgent: (projectId, payload) =>
        request('POST', `/api/projects/${projectId}/agents/generate`, payload),
    getMemorySelection: (projectId) =>
        request('GET', `/api/projects/${projectId}/memory-selection`),
    updateMemorySelection: (projectId, selection) =>
        request('PUT', `/api/projects/${projectId}/memory-selection`, selection),
    analyseRepo: (projectId, target) =>
        request('POST', `/api/projects/${projectId}/analyse-repo`, { target }),
    setClientColour: (clientId, colour) =>
        request('PUT', `/api/clients/${clientId}/colour`, { colour }),
    listClientRepos: (clientId) =>
        request('GET', `/api/clients/${clientId}/repos`),
    addClientRepo: (clientId, name, path) =>
        request('POST', `/api/clients/${clientId}/repos`, { name, path }),
    removeClientRepo: (clientId, repoId) =>
        request('DELETE', `/api/clients/${clientId}/repos/${repoId}`),
    getPlan: (projectId) =>
        request('GET', `/api/projects/${projectId}/plan`),
    addPlanStep: (projectId, payload) =>
        request('POST', `/api/projects/${projectId}/plan/steps`, payload),
    updatePlanStep: (projectId, stepId, payload) =>
        request('PUT', `/api/projects/${projectId}/plan/steps/${stepId}`, payload),
    deletePlanStep: (projectId, stepId) =>
        request('DELETE', `/api/projects/${projectId}/plan/steps/${stepId}`),
    reorderPlanSteps: (projectId, stepIds) =>
        request('PUT', `/api/projects/${projectId}/plan/order`, { stepIds }),
    verifyPlan: (projectId) =>
        request('POST', `/api/projects/${projectId}/plan/verify`),
    executePlanStep: (projectId, stepId) =>
        request('POST', `/api/projects/${projectId}/plan/steps/${stepId}/execute`),
    listStepReviews: (projectId) =>
        request('GET', `/api/projects/${projectId}/step-reviews`),
    getStepReview: (projectId, reviewId) =>
        request('GET', `/api/projects/${projectId}/step-reviews/${reviewId}`),
    commitReviewFile: (projectId, reviewId, path, message) =>
        request('POST', `/api/projects/${projectId}/step-reviews/${reviewId}/commit`, { path, message }),
    rollbackReviewFile: (projectId, reviewId, path) =>
        request('POST', `/api/projects/${projectId}/step-reviews/${reviewId}/rollback`, { path }),
    getSettings: () =>
        request('GET', '/api/settings'),
    updateSettings: (payload) =>
        request('PUT', '/api/settings', payload),
    summariseMemorySection: (projectId, section) =>
        request('POST', `/api/projects/${projectId}/memory-summary`, { section })
};
