# Spec Delta

## Purpose

Defines how application code maps values from one object onto another through registered mapping profiles, so handlers can copy and convert data without hand-written property assignments and new profiles can be added without changing the mapper itself.

## ADDED Requirements

### Requirement: Map onto an existing destination
The system SHALL copy mapped members from a source object onto a destination object supplied by the caller, mutating that destination and returning it, and SHALL leave destination members that no rule maps unchanged.

#### Scenario: Edit copies fields onto a tracked entity
- **WHEN** a caller maps an `Event` source onto an existing `Event` destination using the registered `Event` → `Event` map
- **THEN** every matching member of the destination equals the source's value, and the destination remains the same object instance

#### Scenario: Unmapped destination members are preserved
- **WHEN** a profile marks a destination member as ignored and the caller maps onto an existing destination
- **THEN** that member keeps the value it had before the call

### Requirement: Map into a new destination
The system SHALL create and return a new destination instance populated from the source when the caller does not supply a destination.

#### Scenario: New instance from source
- **WHEN** a caller maps a source object to a destination type that has a registered map and a parameterless construction path
- **THEN** a new destination instance is returned with mapped members populated, distinct from the source

#### Scenario: Null source
- **WHEN** a caller passes a null source
- **THEN** the system rejects the call with an argument error rather than returning a partially built object

### Requirement: Convention-based member matching
The system SHALL, for a registered map, copy each publicly writable destination member that has a readable source member with the same name and an assignable type, without a per-member rule.

#### Scenario: Same-name members are copied automatically
- **WHEN** a map is registered between two types that share member names and types
- **THEN** those members are mapped with no additional configuration

#### Scenario: Members without a source or a setter are skipped
- **WHEN** a destination member has no same-named source member and no override rule, or has no public setter
- **THEN** the member is left unmapped and no error is raised

### Requirement: Per-member overrides
The system SHALL let a profile override how a single destination member is filled, either by computing it from the source or by ignoring it, and an override SHALL take precedence over convention matching for that member.

#### Scenario: Computed member
- **WHEN** a profile declares that destination member `X` is computed from an expression over the source
- **THEN** mapping sets `X` to the expression's result, even if the source also has a member named `X`

#### Scenario: Ignored member
- **WHEN** a profile declares that destination member `X` is ignored
- **THEN** mapping never writes `X`

### Requirement: Nested and collection members
The system SHALL map a member whose source and destination types differ by using the registered map for those types, and SHALL map a collection member element by element using the map for the element types.

#### Scenario: Nested object with its own map
- **WHEN** a member's source and destination types differ and a map is registered between them
- **THEN** the destination member is populated by that map

#### Scenario: Collection of mapped elements
- **WHEN** a source member is a collection whose element type has a registered map to the destination's element type
- **THEN** the destination member is a collection with one mapped element per source element, in the same order

### Requirement: Profile discovery and registration
The system SHALL discover every mapping profile in a given assembly at application startup and make all their maps available through a single injectable mapper, so adding a profile requires no change to registration code.

#### Scenario: New profile is picked up
- **WHEN** a new profile class is added to the scanned assembly
- **THEN** its maps are available to callers after the next startup with no other code changes

#### Scenario: Startup with the existing profile
- **WHEN** the application starts with the current profile that maps `Event` to `Event`
- **THEN** the mapper is resolvable from dependency injection and the map is available

### Requirement: Unregistered and duplicate maps fail clearly
The system SHALL fail with an error that names the source and destination types when a caller requests a map that no profile registered, and SHALL fail at startup when two profiles register the same source/destination pair.

#### Scenario: Missing map
- **WHEN** a caller maps between two types that have no registered map
- **THEN** the call fails with an error naming both types

#### Scenario: Duplicate map
- **WHEN** two profiles register a map for the same source and destination types
- **THEN** application startup fails with an error naming the pair

### Requirement: Edit behavior is preserved
The system SHALL keep the observable result of editing an event unchanged by the mapper replacement: editing an existing event updates its stored fields from the request body and persists them.

#### Scenario: Edit persists mapped fields
- **WHEN** a client edits an existing event with new field values
- **THEN** the stored event reflects those values after the request completes
