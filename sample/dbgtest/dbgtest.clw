  PROGRAM

  MAP
Compute  PROCEDURE(LONG pCount),LONG
  END

GblCount    LONG
GblName     STRING(32)
GblPrice    DECIMAL(9,2)
Person      GROUP
Age           LONG
PersonName    STRING(20)
            END

  CODE
  GblCount = 5
  GblName = 'Hello Clarion'
  GblPrice = 19.99
  Person.Age = 42
  Person.PersonName = 'Roberto'
  GblCount = Compute(GblCount)
  HALT

Compute  PROCEDURE(LONG pCount)
LocSum      LONG
LocIdx      LONG
  CODE
  LocSum = 0
  LOOP LocIdx = 1 TO pCount
    LocSum += LocIdx
  END
  RETURN LocSum
