// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

$(function() {
    tryRefreshUserPermissions();

    // Initialize event handlers
    initializeEventHandlers();

    globals.getWorkspaces();
});