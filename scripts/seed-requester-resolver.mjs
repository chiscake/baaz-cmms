/**
 * Подбор заявителя для demo-seed: asset.location_id ∈ scope заявителя (предок в дереве locations).
 */

/**
 * @param {readonly { id: string; parent_id: string | null }[]} locations
 */
export function buildLocationParentMap(locations) {
  const parentById = new Map();
  for (const location of locations) {
    parentById.set(location.id, location.parent_id ?? null);
  }
  return parentById;
}

/**
 * @param {string} locationId
 * @param {Map<string, string | null>} parentById
 */
export function getLocationDepth(locationId, parentById) {
  let depth = 0;
  let current = locationId;
  while (current) {
    depth += 1;
    current = parentById.get(current) ?? null;
  }
  return depth;
}

/** Корень корпуса: …-BBB000000000 из UUID локации. */
export function getBuildingRootId(locationId) {
  const suffix = locationId.split("-").at(-1) ?? "";
  const building = suffix.slice(0, 3);
  return `a1000000-0000-4000-8000-${building}000000000`;
}

/**
 * @param {string} assetLocationId
 * @param {string} scopeLocationId
 * @param {Map<string, string | null>} parentById
 */
export function isLocationUnderScope(assetLocationId, scopeLocationId, parentById) {
  let current = assetLocationId;
  while (current) {
    if (current === scopeLocationId) {
      return true;
    }
    current = parentById.get(current) ?? null;
  }
  return false;
}

/**
 * @param {readonly { email: string; scopeLocationIds: readonly string[] }[]} requesters
 * @param {Map<string, string | null>} parentById
 */
export function buildRequesterIndex(requesters, parentById) {
  return requesters.map((requester) => ({
    email: requester.email,
    scopes: requester.scopeLocationIds.map((scopeLocationId) => ({
      scopeLocationId,
      depth: getLocationDepth(scopeLocationId, parentById),
      buildingRootId: getBuildingRootId(scopeLocationId),
    })),
  }));
}

/**
 * @param {object} params
 * @param {string | null | undefined} params.assetLocationId
 * @param {string | undefined} params.explicitEmail
 * @param {ReturnType<typeof buildRequesterIndex>} params.requesterIndex
 * @param {Map<string, string | null>} params.parentById
 * @param {string} params.fallbackEmail
 */
export function resolveRequesterEmail({
  assetLocationId,
  explicitEmail,
  requesterIndex,
  parentById,
  fallbackEmail,
}) {
  if (explicitEmail) {
    return explicitEmail;
  }

  if (!assetLocationId) {
    return fallbackEmail;
  }

  const strictMatches = [];
  for (const requester of requesterIndex) {
    for (const scope of requester.scopes) {
      if (isLocationUnderScope(assetLocationId, scope.scopeLocationId, parentById)) {
        strictMatches.push({
          email: requester.email,
          depth: scope.depth,
        });
      }
    }
  }

  if (strictMatches.length > 0) {
    strictMatches.sort((a, b) => b.depth - a.depth);
    return strictMatches[0].email;
  }

  const assetBuildingRootId = getBuildingRootId(assetLocationId);
  const buildingMatches = [];
  for (const requester of requesterIndex) {
    for (const scope of requester.scopes) {
      if (scope.buildingRootId === assetBuildingRootId) {
        buildingMatches.push({
          email: requester.email,
          depth: scope.depth,
        });
      }
    }
  }

  if (buildingMatches.length > 0) {
    buildingMatches.sort((a, b) => b.depth - a.depth);
    return buildingMatches[0].email;
  }

  return fallbackEmail;
}

/**
 * @param {readonly { email: string; role: string; locationScopeCodes?: readonly string[] }[]} demoUsers
 * @param {Map<string, string>} locationsByCode
 */
export function buildRequesterCandidates(demoUsers, locationsByCode) {
  return demoUsers
    .filter((user) => user.role === "requester")
    .map((user) => ({
      email: user.email,
      scopeLocationIds: (user.locationScopeCodes ?? []).map((code) => {
        const locationId = locationsByCode.get(code);
        if (!locationId) {
          throw new Error(`Unknown location scope code for requester ${user.email}: ${code}`);
        }
        return locationId;
      }),
    }));
}
