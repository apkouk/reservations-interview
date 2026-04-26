import { useState } from "react";
import {
  Badge,
  Flex,
  Heading,
  Section,
  Switch,
  Table,
  Text,
} from "@radix-ui/themes";
import { useGetStaffReservations } from "./api";
import { LoadingCard } from "../components/LoadingCard";

export function StaffReservationsPage() {
  const { isLoading, data: reservations, isError } = useGetStaffReservations();
  const [todayOnly, setTodayOnly] = useState(false);

  const today = new Date().toLocaleDateString();

  const filtered = reservations?.filter((r) =>
    todayOnly ? new Date(r.start).toLocaleDateString() === today : true
  );

  return (
    <Section size="2" px="4">
      <Flex align="center" justify="between" mb="6">
        <Heading size="8" as="h1" color="mint">
          Upcoming Reservations
        </Heading>
        <Flex align="center" gap="2">
          <Text size="2">Today only</Text>
          <Switch checked={todayOnly} onCheckedChange={setTodayOnly} />
        </Flex>
      </Flex>

      {isLoading && <LoadingCard />}

      {isError && (
        <Text color="red">Failed to load reservations. Please log in again.</Text>
      )}

      {filtered && filtered.length === 0 && (
        <Text color="gray">No reservations found.</Text>
      )}

      {filtered && filtered.length > 0 && (
        <Table.Root variant="surface">
          <Table.Header>
            <Table.Row>
              <Table.ColumnHeaderCell>Room</Table.ColumnHeaderCell>
              <Table.ColumnHeaderCell>Guest Email</Table.ColumnHeaderCell>
              <Table.ColumnHeaderCell>Start</Table.ColumnHeaderCell>
              <Table.ColumnHeaderCell>End</Table.ColumnHeaderCell>
              <Table.ColumnHeaderCell>Status</Table.ColumnHeaderCell>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {filtered.map((r) => (
              <Table.Row key={r.id}>
                <Table.Cell>#{r.roomNumber}</Table.Cell>
                <Table.Cell>{r.guestEmail}</Table.Cell>
                <Table.Cell>{new Date(r.start).toLocaleDateString()}</Table.Cell>
                <Table.Cell>{new Date(r.end).toLocaleDateString()}</Table.Cell>
                <Table.Cell>
                  <Flex gap="1">
                    {r.checkedIn && <Badge color="green">Checked In</Badge>}
                    {r.checkedOut && <Badge color="gray">Checked Out</Badge>}
                    {!r.checkedIn && !r.checkedOut && <Badge color="blue">Upcoming</Badge>}
                  </Flex>
                </Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table.Root>
      )}
    </Section>
  );
}
