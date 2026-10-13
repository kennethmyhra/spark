import http from 'k6/http';
import exec from 'k6/execution';
import { check } from 'k6';
import { patient } from './patients.js';
import {
  encounter, encounterValues, observations, organization, ORGANIZATIONS, practitioner, PRACTITIONERS,
} from './clinical.js';

const COUNT = parseInt(__ENV.COUNT || '100000');
// Each patient also gets an encounter and its vital sign and laboratory observations, unless CLINICAL is false.
const CLINICAL = (__ENV.CLINICAL || 'true') === 'true';

export const options = {
  scenarios: {
    load: { executor: 'shared-iterations', vus: parseInt(__ENV.VUS || '50'), iterations: COUNT, maxDuration: __ENV.MAX_DURATION || '180m' },
  },
};

const params = { headers: { 'Content-Type': 'application/fhir+json', 'Accept': 'application/fhir+json' } };

function post(type, body, name) {
  const res = http.post(`${__ENV.BASE_URL}/${type}`, JSON.stringify(body), { ...params, tags: { name } });
  check(res, { [`${type} created`]: (r) => r.status === 201 });
  return res.status === 201 ? `${type}/${res.json().id}` : null;
}

// The practitioners and organizations the encounters and observations refer to, created once before the patients.
export function setup() {
  if (!CLINICAL) return {};
  return {
    practitioners: Array.from({ length: PRACTITIONERS }, (_, n) => post('Practitioner', practitioner(n), 'Practitioner')),
    organizations: Array.from({ length: ORGANIZATIONS }, (_, n) => post('Organization', organization(n), 'Organization')),
  };
}

export default function (data) {
  const i = exec.scenario.iterationInTest;
  const patientReference = post('Patient', patient(i), 'Patient');
  if (!CLINICAL || patientReference === null) return;

  const v = encounterValues(i);
  const practitionerReference = data.practitioners[v.practitioner];
  const encounterReference = post('Encounter',
    encounter(i, patientReference, practitionerReference, data.organizations[v.organization]), 'Encounter');
  if (encounterReference === null) return;

  for (const body of observations(i, patientReference, encounterReference, practitionerReference)) {
    post('Observation', body, 'Observation');
  }
}
