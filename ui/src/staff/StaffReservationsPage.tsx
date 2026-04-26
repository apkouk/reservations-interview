import { useState } from "react";
import {
  Badge,
  Box,
  Button,
  Dialog,
  Flex,
  Heading,
  Section,
  Switch,
  Table,
  Text,
  TextField,
} from "@radix-ui/themes";
import { useGetStaffReservations, useCheckIn, StaffReservation } from "./api";
import { LoadingCard } from "../components/LoadingCard";

function CheckInDialog({
  reservation,
  onClose,
}: {
  reservation: StaffReservation;
  onClose: () => void;
}) {
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const checkIn = useCheckIn();

  async function handleConfirm() {
    setError("");
    try {
      await checkIn.mutateAsync({ reservationId: reservation.id, guestEmail: email });
      onClose();
    } catch {
      setError("Check-in failed. Please verify the email and try again.");
    }
  }

  return (
    <Dialog.Content size="3">
      <Dialog.Title>Check In - Room #{reservation.roomNumber}</Dialog.Title>
      <Dialog.Description mb="4">
        Enter the guest email address to confirm check-in.
      </Dialog.Description>
      <Flex direction="column" gap="3">
        <Box>
          <Text as="label" size="2" weight="medium" htmlFor="confirm-email">
            Guest Email
          </Text>
          <TextField.Root
            id="confirm-email"
            type="email"
            placeholder={reservation.guestEmail}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            mt="1"
          />
        </Box>
        {error && (
          <Text color="red" size="2">
            {error}
          </Text>
        )}
        <Flex gap="3" justify="end">
          <Dialog.Close>
            <Button variant="soft" color="gray" onClick={onClose}>
              Cancel
            </Button>
          </Dialog.Close>
          <Button onClick={handleConfirm} disabled={!email || checkIn.isPending}>
            {checkIn.isPending ? "Checking in..." : "Confirm Check In"}
          </Button>
        </Flex>
      </Flex>
    </Dialog.Content>
  );
}

export function StaffReservationsPage() {
  const { isLoading, data: reservations, isError } = useGetStaffReservations();
  const [todayOnly, setTodayOnly] = useState(false);
  const [selectedReservation, setSelectedReservation] = useState<StaffReservation | null>(null);

  const today = new Date().toLocaleDateString();

  const isToday = (dateStr: string) => new Date(dateStr).toLocaleDateString() === today;

  const filtered = reservations?.filter((r) =>
    todayOnly ? isToday(r.start) : true
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
        <Dialog.Root
          open={!!selectedReservation}
          onOpenChange={(open) => { if (!open) setSelectedReservation(null); }}
        >
          <Table.Root variant="surface">
            <Table.Header>
              <Table.Row>
                <Table.ColumnHeaderCell>Room</Table.ColumnHeaderCell>
                <Table.ColumnHeaderCell>Guest Email</Table.ColumnHeaderCell>
                <Table.ColumnHeaderCell>Start</Table.ColumnHeaderCell>
                <Table.ColumnHeaderCell>End</Table.ColumnHeaderCell>
                <Table.ColumnHeaderCell>Status</Table.ColumnHeaderCell>
                <Table.ColumnHeaderCell />
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
                  <Table.Cell>
                    {!r.checkedIn && isToday(r.start) && (
                      <Button size="1" onClick={() => setSelectedReservation(r)}>
                        Check In
                      </Button>
                    )}
                  </Table.Cell>
                </Table.Row>
              ))}
            </Table.Body>
          </Table.Root>

          {selectedReservation && (
            <CheckInDialog
              reservation={selectedReservation}
              onClose={() => setSelectedReservation(null)}
            />
          )}
        </Dialog.Root>
      )}             
    </Section>
  );
}
