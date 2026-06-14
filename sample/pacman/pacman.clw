
  PROGRAM

OMIT('***')
 * Created with Clarion 11.1
 * User: robir
 * Date: 2/3/2025
 * Time: 8:45 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 ***

  MAP
  END



WINDOW('Pac-Man Clone'),AT(,,400,400),SYSTEM,GRAY
    PICTURE,AT(175,175,20,20),USE(?PacMan),BITMAP('pacman.bmp')  ! Pac-Man
    PICTURE,AT(100,100,20,20),USE(?Ghost1),BITMAP('ghost.bmp')   ! Ghost
    BOX,AT(50,50,300,300),USE(?Box1)  ! Outer wall
    BOX,AT(150,150,50,50),USE(?Box2)  ! Inner wall
END

code    

ACCEPT
    CASE EVENT()
    OF EVENT:KeyDown
        CASE ACCEPTED()
        OF 37  ! Left arrow
            IF ~COLLISION(?PacMan,?Box1) AND ~COLLISION(?PacMan,?Box2)
                MOVE(?PacMan,LEFT,10)
            END
        OF 38  ! Up arrow
            IF ~COLLISION(?PacMan,?Box1) AND ~COLLISION(?PacMan,?Box2)
                MOVE(?PacMan,UP,10)
            END
        OF 39  ! Right arrow
            IF ~COLLISION(?PacMan,?Box1) AND ~COLLISION(?PacMan,?Box2)
                MOVE(?PacMan,RIGHT,10)
            END
        OF 40  ! Down arrow
            IF ~COLLISION(?PacMan,?Box1) AND ~COLLISION(?PacMan,?Box2)
                MOVE(?PacMan,DOWN,10)
            END
        END
    END

    ! Ghost movement (simple chase)
    IF X(?Ghost1) < X(?PacMan)
        MOVE(?Ghost1,RIGHT,5)
    ELSE
        MOVE(?Ghost1,LEFT,5)
    END
    IF Y(?Ghost1) < Y(?PacMan)
        MOVE(?Ghost1,DOWN,5)
    ELSE
        MOVE(?Ghost1,UP,5)
    END

    ! Collision detection with ghost
    IF COLLISION(?PacMan,?Ghost1)
        MESSAGE('Game Over! Pac-Man was caught by the ghost!')
        BREAK
    END
END