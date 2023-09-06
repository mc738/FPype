namespace FPype.Infrastructure.Scheduling

module Models =

    type NewSchedule =
        { PipelineVersionReference: string
          ScheduleCron: string }


    type ScheduleOverview =
        { Reference: string
          SubscriptionId: string
          PipelineVersionReference: string
          ScheduleCron: string }
