﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace RockGarden
{
    class Garden
    {
        public static int n_directions = 8; //The four cardinal directions + NE, NW, SW, SE.
        public const int NORTH = 0, NORTHEAST = 1, EAST = 2, SOUTHEAST = 3, SOUTH = 4, SOUTHWEST = 5, WEST = 6, NORTHWEST = 7;
        public int length { get; set; }
        public int width { get; set; }
        private Atom[,] grid;
        public List<Resident> residentsList { get; set; }

        /// <summary>
        /// Create a Garden of length and width.
        /// </summary>
        public Garden(int length, int width)
        {
            residentsList = new List<Resident>();
            this.length = length;
            this.width = width;
            grid = new Atom[width, length];
            foreach (Point coordinate in getCoordinates())
            {
                grid[coordinate.X, coordinate.Y] = new Atom(coordinate);
                grid[coordinate.X, coordinate.Y].rockHeuristic = 1;
            }
            setHeuristic();
        }

        /// <summary>
        /// Return the Resident that occupies this coordinate pair.
        /// </summary>
        /// <param name="x">X portion of the coordinate pair.</param>
        /// <param name="y">Y portion of the coordinate pair.</param>
        /// <returns>The Resident that occupies this square</returns>
        public Resident getResident(Point spot)
        {
            return atomAt(spot).getResident();
        }
        /// <summary>
        /// Fills empty atoms in the garden with blank gravel.
        /// </summary>
        public void fillWithGravel()
        {
            //If bordered by Rock, set Heuristic to a low value
            Atom tempAtom;
            Resident tempResident;
            Point origin, ringOrigin;
            int length, width;
            List<Point> coordinatesRing;
            List<Point> alreadyEncountered = new List<Point>();
            foreach (Point coordinate in getCoordinates())
            {
                tempAtom = atomAt(coordinate);
                if (!alreadyEncountered.Contains(coordinate))
                {
                    alreadyEncountered.Add(coordinate);
                    if (tempAtom.getResident() == null)
                    {
                        tempAtom.setResident(new Gravel(),
                        coordinate);
                    }
                    else
                    {
                        tempResident = tempAtom.getResident();
                        origin = tempResident.origin;
                        if (!alreadyEncountered.Contains(origin) && tempResident.ToString().Equals(Rock.rockString))
                        {
                            alreadyEncountered.Add(origin);
                            ringOrigin = new Point(origin.X, origin.Y);
                            length = tempResident.length + 2;
                            width = tempResident.width + 2;
                            coordinatesRing = getCoordinates(ringOrigin, width, length);
                            for (int i = coordinatesRing.Count - 1; i >= 0; i--)
                            {
                                //check if the point is within the rock
                                if (inGarden(coordinatesRing[i]) && !tempResident.isInResident(coordinatesRing[i]) && atomAt(coordinatesRing[i]).getResident() != null && atomAt(coordinatesRing[i]).getResident().isGravel)
                                {
                                    //set this atom to the rock border heuristic cost
                                    if (!alreadyEncountered.Contains(coordinatesRing[i]))
                                    {
                                        atomAt(coordinatesRing[i]).setResident(new Gravel(),
                                        coordinate);
                                        alreadyEncountered.Add(coordinatesRing[i]);
                                    }
                                    atomAt(coordinatesRing[i]).rockBorder();
                                }

                            }
                        }
                    }
                }
            }
        }
        private void setHeuristic()
        {
            double limX = 0.2;
            double limY = 0.2;
            Point temp;
            foreach (Atom a in grid)
            {
                a.rockBorderHeuristic = 1;
            }
            for (int x = 0; x < limX * width; x++)
            {
                for(int y = 0; y < length; y++)
                {
                    temp = new Point(x, y);
                    atomAt(temp).rockBorderHeuristic = 0.1;
                    temp = new Point(width - x, y);
                    atomAt(temp).rockBorderHeuristic = 0.1;
                }
            }
            for (int y = 0; y < limY * length; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    temp = new Point(x, y);
                    atomAt(temp).rockBorderHeuristic = 0.1;
                    temp = new Point(x, y - length);
                    atomAt(temp).rockBorderHeuristic = 0.1;
                }
            }

        }
        public bool inGarden(Point check)
        {
            return check.Y >= 0 && check.X >= 0 && check.X < width && check.Y < length;
        }
        /// <summary>
        /// Adds a Resident to the garden.
        /// </summary>
        /// <param name="newNeighbor">The Resident to be added to the garden.</param>
        /// <param name="x">The leftmost x coordinate where the Resident will be placed.</param>
        /// <param name="y">The bottommost y coordinate where the Resident will be placed.</param>
        /// <param name="overwrite">Boolean indicating if the object will remove any object that
        /// may be in its intended footprint.</param>
        /// <returns>A boolean indicating whether the addition was successful or not.</returns>
        public bool addResident(Resident newNeighbor, Point spot, bool overwrite)
        {
            
            newNeighbor.origin = spot;
            newNeighbor.hueristic = atomAt(newNeighbor.center).rockHeuristic;
            if (!(inGarden(spot) && inGarden(new Point(spot.X + newNeighbor.width, spot.Y + newNeighbor.length))))
            {
                return false;
            }
            residentsList.Add(newNeighbor);
            List<Point> pointsToAddTo = getCoordinates(spot, newNeighbor.width, newNeighbor.length);
            //check if we can add the resident to the grid
            //return false if user does not want to overwrite anything that isn't gravel
            //remove that object if it is.
            foreach (Point coordinate in pointsToAddTo)
            {
                //is there an item here? is it gravel?
                if (getResident(coordinate) != null &&
                    !getResident(coordinate).isGravel)
                {
                    if (!overwrite)
                    {
                        //Since there is a Resident here, we must throw false to show that the addition won't happen
                        return false;
                    }
                    else
                    {
                        //delete the object here at all locations it exists
                        removeResident(spot);
                    }
                }
            }
            //add the resident to the grid at each point it would occupy based on its size
            foreach (Point coordinate in pointsToAddTo)
            {
                atomAt(coordinate).setResident(newNeighbor, spot);
            }
            return true;
        }
        /// <summary>
        /// Removes any object that occupies this point on the grid.
        /// Will remove all instances of this object.
        /// </summary>
        /// <param name="x">Any x coordinate of the object to be removed so long as the y
        /// coodinate that forms the pair (x, y) contains the Resident to be removed.
        /// </param>
        /// <param name="y">Any y coordinate of the object to be removed so long as the x
        /// coodinate that forms the pair (x, y) contains the Resident to be removed.</param>
        /// <returns>Boolean indicating whether any object existed on the passed set of
        /// coordinates.</returns>
        public bool removeResident(Point spot)
        {
            Resident evictee = atomAt(spot).getResident();
            residentsList.Remove(evictee);
            if (evictee == null)
            {
                //no Resident occupies this space
                return false;
            }
            //go through each Atom that contains this object
            foreach (Point coordinate in getCoordinates(atomAt(spot).getLocation(),
                evictee.width, evictee.length))
            {
                atomAt(coordinate).removeResident();
            }
            return true;
        }
        /// <summary>
        /// A river is a multiple Atom wide stream.
        /// The list of Points must have the same counts.
        /// </summary>
        /// <param name="startPoints">A list of Points for the river to start at.</param>
        /// <param name="endPoints">A list of Points for the river to end at.</param>
        public void addRiver(List<Point> startPoints, List<Point> endPoints)
        {
            if (startPoints.Count != endPoints.Count)
            {
                throw new DataMisalignedException();
            }
            for (int index = 0; index < startPoints.Count; index++)
            {
                addStream(startPoints[index], endPoints[index]);
            }
        }

        /// <summary>
        /// Creates a stream between two points. Will take the direction between the two and
        /// move one-by-one until the points meet.
        /// </summary>
        /// <param name="start.X">The x-coordinate from the first point.</param>
        /// <param name="start.Y">The y-coordinate from the first point.</param>
        /// <param name="end.X">The x-coordinate from the second point.</param>
        /// <param name="end.Y">The y-coordinate from the second point.</param>
        public void addStream(Point start, Point end)
        {
            if (!atomAt(start).getResident().isGravel || !atomAt(end).getResident().isGravel)
            {
                return;
            }
            int max = 500, runs = 0;
            Point new_end = end, new_start = start, old_start;
            int direction;
            while (new_start.X != new_end.X && new_start.Y != new_end.Y && runs < max)
            {
                direction = closestDirection(new_start, new_end);
                old_start = new Point(new_start.X, new_start.Y);
                //getting the new coordinate from the radians
                directionSwitch(ref new_start, direction, false, 0);
                runs++;
            }
        }
        public void addStreamAStar(Point start, Point end)
        {
            Atom startAtom = atomAt(start);
            Atom targetAtom = atomAt(end);
            List<Atom> openSet = new List<Atom>();
            HashSet<Atom> closeSet = new HashSet<Atom>();
            openSet.Add(startAtom);

            while (openSet.Count > 0)
            {
                Atom currentAtom = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentAtom.fCost || openSet[i].fCost == currentAtom.fCost
                        && openSet[i].rockHeuristic < currentAtom.rockHeuristic)
                    {
                        currentAtom = openSet[i];
                    }
                }

                openSet.Remove(currentAtom);
                closeSet.Add(currentAtom);

                if (currentAtom == targetAtom)
                {
                    TracePath(startAtom, targetAtom);
                    return;
                }

                foreach (Atom neighbor in getNeighbors(currentAtom))
                {
                    if (neighbor == null || neighbor.getResident() == null || (!neighbor.getResident().isGravel || closeSet.Contains(neighbor)))
                    {
                        continue;
                    }

                    double newMovementCostToNeighbor = currentAtom.gCost + getDistance(currentAtom, neighbor);
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.rockHeuristic = getDistance(neighbor, targetAtom);
                        neighbor.parent = currentAtom;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
        }
        private List<Atom> getNeighbors(Atom MrRodgers)
        {
            List<Atom> wontYouBeMyNeighbor = new List<Atom>();
            Point hisHome = MrRodgers.getLocation();
            for (int xDelta = -1; xDelta <= 1; xDelta++)
            {
                for (int yDelta = -1; yDelta <= 1; yDelta++)
                {
                    if (yDelta == 0 && xDelta == 0)
                    {
                        continue;
                    }
                    wontYouBeMyNeighbor.Add(atomAt(new Point(hisHome.X + xDelta, hisHome.Y + yDelta)));
                }
            }
            return wontYouBeMyNeighbor;
        }
        private void TracePath(Atom startNode, Atom endNode)
        {
            Atom last = endNode;
            Atom next;
            while(last.parent != null)
            {
                next = last.parent;
                addStreamSingular(last.getLocation(), closestDirection(last.getLocation(), next.getLocation()));
                addStreamSingular(next.getLocation(), closestDirection(next.getLocation(), last.getLocation()));
                last = next;
            }

            ///Instantiate(cube, nodes in the path)
        }
        private bool directionSwitch(ref Point new_start, int direction, bool positive, int delta)
        {
            if (positive)
            {
                direction += delta;
            }
            else
            {
                direction -= delta;
            }
            delta += 1;
            Point old_start = new Point(new_start.X, new_start.Y);
            bool overrideLegal = direction < 0 || direction > n_directions - 1;
            //recursion limit
            if (delta > 16)
            {
                return false;
            }
            //assigning movement based on direction only if direction is between 0 and 8
            if (!overrideLegal)
            {
                new_start = moveByDirection(new_start, direction);
            }
            //try to add stream to this location and if it fails start trying to 'wiggle' around
            if (!addStreamSingular(new_start, direction) || overrideLegal)
            {
                new_start = new Point(old_start.X, old_start.Y);
                return directionSwitch(ref new_start, direction, positive ^= true, delta);
            }
            //add a stream to the last spot in the opposite direction
            addStreamSingular(old_start, oppositeDirection(direction));
            return true;
        }
        private bool addStreamSingular(Point spot, int direction)
        {
            return atomAt(spot).getResident().addStream(direction);
        }

        /// <summary>
        /// Determines if there is a stream between the two Residents.
        /// A line will be drawn from the centers of the residents and a stream will be searched
        /// for along that path. Any stream on that path will cause this method to evaluate to 
        /// true. If no streams are found, the method will return false.
        /// </summary>
        /// <param name="start">The Resident from which the search will begin.</param>
        /// <param name="end">The Resident on which the search will end.</param>
        /// <returns>The existence of a stream between the two Residents.</returns>
        public bool isStreamBetween(Resident start, Resident end)
        {
            return isSearchBetween(detectStream, start, end);
        }
        /// <summary>
        /// Determines if there is a Rock between the two Residents.
        /// A line will be drawn from the centers of the residents and a Rock will be searched
        /// for along that path. Any Rock on that path will cause this method to evaluate to 
        /// true. If no Rocks are found, the method will return false.
        /// </summary>
        /// <param name="start">The Resident from which the search will begin.</param>
        /// <param name="end">The Resident on which the search will end.</param>
        /// <returns>The existence of a Rock between the two Residents.</returns>
        public bool isRockBetween(Resident start, Resident end)
        {
            return isSearchBetween(detectRock, start, end);
        }
        private bool isSearchBetween(Func<Resident, bool> detectionMethod, Resident start, Resident end)
        {

            int direction;
            Resident tempRes;
            Point floatingPoint = start.center;
            while (!floatingPoint.Equals(end.center))
            {
                tempRes = atomAt(floatingPoint).getResident();
                if (detectionMethod(tempRes))
                {
                    return true;
                }
                direction = closestDirection(floatingPoint, end.center);
                floatingPoint = moveByDirection(floatingPoint, direction);
            }
            //no stream found after all points between searched.
            return false;
        }

        /// <summary>
        /// Returns a boolean on the existence of a rock on tempRes.
        /// </summary>
        /// <param name="tempRes">The Resident to be examined for a rock.</param>
        /// <returns>A bool on the existence of a rock at tempRes.</returns>
        private bool detectRock(Resident tempRes)
        {            
            return !tempRes.isGravel;
        }

        /// <summary>
        /// Returns a boolean on the existence of a stream on tempRes.
        /// </summary>
        /// <param name="tempRes">The Resident to be examined for a stream.</param>
        /// <returns>A bool on the existence of a stream at tempRes.</returns>
        private bool detectStream(Resident tempRes)
        {
            return tempRes.hasStream;
        }
        public static Point moveByDirection(Point basePoint, int direction)
        {
            switch (direction)
            {
                case NORTH:
                    basePoint.X += 0;
                    basePoint.Y += 1;
                    break;
                case NORTHEAST:
                    basePoint.X += 1;
                    basePoint.Y += 1;
                    break;
                case EAST:
                    basePoint.X += 1;
                    basePoint.Y += 0;
                    break;
                case SOUTHEAST:
                    basePoint.X += 1;
                    basePoint.Y -= 1;
                    break;
                case SOUTH:
                    basePoint.X += 0;
                    basePoint.Y -= 1;
                    break;
                case SOUTHWEST:
                    basePoint.X -= 1;
                    basePoint.Y -= 1;
                    break;
                case WEST:
                    basePoint.X -= 1;
                    basePoint.Y += 0;
                    break;
                case NORTHWEST:
                    basePoint.X -= 1;
                    basePoint.Y += 1;
                    break;
            }
            return basePoint;
        }
        public static int closestDirection(Point start, Point end)
        {
            int endX_toOrigin = end.X - start.X;
            int endY_toOrigin = end.Y - start.Y;
            //arguments flipped to reflect 0 being north instead of east
            double angle_in_radians = Math.Atan2(endX_toOrigin, endY_toOrigin) * (4 / Math.PI);
            if (angle_in_radians < 0)
            {
                angle_in_radians = (n_directions + angle_in_radians);
            }
            return ((int)Math.Round(angle_in_radians, MidpointRounding.AwayFromZero)) % n_directions;
        }

        /// <summary>
        /// Return a list of coordinates that would be found in a rectangle of width x length size
        /// based at the coordinate (startX, startY).
        /// </summary>
        /// <param name="startX">Base x coordinate</param>
        /// <param name="startY">Base y coordinate</param>
        /// <param name="width">Number of atoms wide the selection is</param>
        /// <param name="length">Number of atoms long the selection is </param>
        /// <returns></returns>
        private static List<Point> getCoordinates(Point start, int width, int length)
        {
            List<Point> points = new List<Point>();
            for (int currentX = start.X; currentX < start.X + width; currentX++)
            {
                for (int currentY = start.Y; currentY < start.Y + length; currentY++)
                {
                    points.Add(new Point(currentX, currentY));
                }
            }
            return points;
        }
        /// <summary>
        /// Get all coordinates of the grid
        /// </summary>
        /// <returns></returns>
        private List<Point> getCoordinates()
        {
            return getCoordinates(new Point(0, 0), width, length);
        }
        /// <summary>
        /// Calculates the opposite number on a clock of 8 hours (our 8 directions)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static int oppositeDirection(int direction)
        {
            return direction > SOUTH ? direction % SOUTH : direction + SOUTH;
        }
        /// <summary>
        /// Gets the Atoms in a specified area.
        /// </summary>
        /// <param name="origin">The lower left corner of the neighborhood.</param>
        /// <param name="x">How many x Atoms the neighborhood spans.</param>
        /// <param name="y">How many y Atoms the neighborhood spans.</param>
        /// <returns>A list of the Atoms in this neighborhood in the form of a Neiughborhood
        /// object.</returns>
        public Neighborhood getNeighborhood(Point origin, int x, int y)
        {
            List<Point> addresses = getCoordinates(origin, x, y);
            //the neighborhood listserv...
            List<Atom> neighbors = new List<Atom>();
            foreach (Point coordinate in addresses)
            {
                neighbors.Add(atomAt(coordinate));
            }
            return new Neighborhood(neighbors);
        }
        /// <summary>
        /// Gets the Atom located at this Point in the Garden.
        /// Used to eliminate clunky use of grid[p.X, p.Y].
        /// </summary>
        /// <param name="location">The location of the Atom desired.</param>
        /// <returns>The Atom at the passed location.</returns>
        public Atom atomAt(Point location)
        {
            if (!inGarden(location))
            {
                return new Atom(new Point(-1, -1));
            }
            try
            {
                return grid[location.X, location.Y];
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("This location: (" + location.X + ", " + location.Y + ") is not in the Garden.");
                return new Atom(new Point(-1, -1));
            }
        }
        public List<Resident> getAllOfType(string type)
        {
            List<Resident> residentOfType = new List<Resident>();
            foreach(Resident testingThisResident in residentsList)
            {
                if(testingThisResident.ToString().Equals(type))
                {
                    residentOfType.Add(testingThisResident);
                }
            }
            return residentOfType;
        }
        /// <summary>
        /// Evaluates the garden for symmetry by checking for relative symmetry on 4 axis
        /// N-S, E-W, NE-SW, and NW-SE.
        ///
        /// </summary>
        /// <returns>A double from 0 (unique) to 1 (full symmetry) describing the symmetry</returns>
        public double symmetric(int jigger, double weight)
        {
            //Gravel matches aren't that big of a deal.
            //More interested in Rocks
            //We are going to take the Hamming distance of the strings by "Jiggering" them by some integer (call it jigger) in each direction
            //Unlike Hamming we are only going to take the count of the Rs that do match up to the other neighborhood.
            //Then we will divide this number by the min number of Rs in each comparsion section times J^2 (so the number of Rs in N for E-W and NE in NW-SE)
            //If perfectly symmetrical, this number will be 1.
            List<double> products = new List<double>();
            Neighborhood[] halves = allHalves();
            Neighborhood north = halves[NORTH], northeast = halves[NORTHEAST], east = halves[EAST],
                                 southeast = halves[SOUTHEAST], south = halves[SOUTH],
                                 southwest = halves[SOUTHWEST], west = halves[WEST],
                                 northwest = halves[NORTHWEST];
            //do all comparisons
            products.Add(symmetric(north, south, jigger, false, false));
            products.Add(symmetric(east, west, jigger, false, true));
            products.Add(symmetric(northwest, southeast, jigger, true, false));
            products.Add(symmetric(northeast, southwest, jigger, true, false));
            double averageProduct = average(products); 
            return averageProduct * weight;
        }
        public Neighborhood[] allHalves()
        {
            Point tempPoint;
            Atom tempAtom;
            double slope = length / (width + 0.0);
            Neighborhood north, south, east, west, northeast, southwest, northwest, southeast;
            north = getNeighborhood(new Point(0, length / 2), width, length - (length / 2));
            south = getNeighborhood(new Point(0, 0), width, (length / 2));
            east = getNeighborhood(new Point(0, 0), (width / 2), length);
            west = getNeighborhood(new Point((width / 2), 0), width - (width / 2), length);
            northeast = new Neighborhood();
            southeast = new Neighborhood();
            northwest = new Neighborhood();
            southwest = new Neighborhood();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    tempPoint = new Point(x, y);
                    tempAtom = atomAt(tempPoint);
                    if (y > slope * x)
                    {
                        //in NW
                        northwest.addAtom(tempAtom);
                    }
                    else
                    {
                        //in SE
                        southeast.addAtom(tempAtom);
                    }
                    if (y > (-1 * slope) * x + length)
                    {
                        //in NE
                        northeast.addAtom(tempAtom);
                    }
                    else
                    {
                        //in SW
                        southwest.addAtom(tempAtom);
                    }
                }
            }
            return new Neighborhood[] { north, northeast, east, southeast, south, southwest, west, northwest };
        }
        /// <summary>
        /// Go through every point in the upper neighborhood and compare it to the lower.
        /// Need to implement how to refernce points (Offsetting)
        /// </summary>
        /// <param name="upper"></param>
        /// <param name="lower"></param>
        /// <param name="jigger"></param>
        /// <returns></returns>
        private double symmetric(Neighborhood upper, Neighborhood lower, int jigger, bool flipCoordinates, bool flipOnWidth)
        {
            int totalRocks;
            int upperRocks = upper.numberOfRocks();
            int lowerRocks = lower.numberOfRocks();
            totalRocks = upperRocks < lowerRocks ? upperRocks : lowerRocks;
            List<double> products = new List<double>();
            Atom tempAtom;
            Point tempLocation;

            foreach (Atom a in upper.getAtoms())
            {
                if (!a.getResident().isGravel)
                {
                    if (flipCoordinates)
                    {
                        tempLocation = invert(a.getLocation());
                    }
                    else
                    {
                        if (flipOnWidth)
                        {
                            tempLocation = new Point(a.getLocation().X, length - 1 - a.getLocation().Y);
                        }
                        else
                        {
                            tempLocation = new Point(width - 1 - a.getLocation().X, a.getLocation().Y);
                        }
                    }
                    tempAtom = atomAt(tempLocation);
                    products.Add(singleSymmetry(tempAtom, lower, jigger, a.getLocation()));
                }
            }
            return 1 - Math.Pow(average(products), 1 / 4.0);
        }
        private double average(List<double> numbers)
        {
            double sum = 0;
            int count = 0;
            foreach (double number in numbers)
            {
                sum += number;
                count += 1;
            }
            return count != 0 ? sum / count : 0;
        }
        /// <summary>
        /// Flips the X and Y.
        /// </summary>
        /// <param name="location">The Point to be inverted.</param>
        /// <returns>The inverted Point.</returns>
        public Point invert(Point location)
        {
            double slope = length / (width + 0.0);
            //y = slope * x
            //x = slope / y
            double xDistanceToLine = location.X - location.Y / slope;
            double yDistanceToLine = location.Y - slope * location.X;
            int newX = (int)Math.Round(slope * location.X - xDistanceToLine, MidpointRounding.AwayFromZero);
            int newY = (int)Math.Round(location.Y / slope - yDistanceToLine, MidpointRounding.AwayFromZero);
            return new Point(newX, newY);
        }
        /// <summary>
        /// Counts the number of hits and return how many hits occured against how many
        /// were possible.
        /// </summary>
        /// <param name="atomCompare"></param>
        /// <param name="neighboorhoodCompare"></param>
        /// <param name="jigger"></param>
        /// <param name="atomLocation"></param>
        /// <returns></returns>
        private static double singleSymmetry(Atom atomCompare, Neighborhood neighboorhoodCompare,
                                             int jigger, Point atomLocation)
        {
            string atomCompareString = atomCompare.ToString(), compareTo;
            //temp. Only care about rocks
            if (!atomCompareString.Equals(Rock.rockString))
            {
                return 0;
            }
            Point comparePoint;
            int countHits = 0, countAll = 0;
            for (int xDelta = -1 * jigger; xDelta <= jigger; xDelta++)
            {
                for (int yDelta = -1 * jigger; yDelta <= jigger; yDelta++)
                {
                    comparePoint = new Point(atomLocation.X + xDelta, atomLocation.Y + yDelta);
                    compareTo = neighboorhoodCompare.getAtom(comparePoint).ToString();
                    try
                    {
                        if (atomCompareString.Equals(compareTo))
                        {
                            countHits++;
                        }
                        countAll++;
                    }
                    catch
                    {
                        //not on the grid. This is instance is not a problem. Jiggering can throw
                        //us outside of the grid.
                    }
                }
            }
            //prevent division by zero errors
            return countAll != 0? countHits / (countAll + 0.0): 0;
        }

        /// <summary>
        /// Get a string representation of the Rock Garden.
        /// </summary>
        /// <returns>A string which gives an ASCII art view of the rock garden</returns>
        public string ToString(bool readable)
        {
            string gardenString = readable ? "   ": "";
            string bottom = "   ";
            if (readable)
            {
                for (int i = 0; i < width; i++)
                {
                    gardenString += i.ToString();
                    if (i < 10)
                    {
                        gardenString += " ";
                    }
                    bottom += "__";
                }

                gardenString += "\n" + bottom + "\n" + length + (length > 10 ? "| " : " | ");
            }
            for (int y = length - 1; y >= 0; y-- )
            {
                //counting down as the (0, 0) is located in the bottom right corner
                for (int x = 0; x < width; x++)
                {
                    gardenString += atomAt(new Point(x, y)).ToString() + " ";
                }
                //going to a new row so we will need a new line

                gardenString += "\n" + (readable ? y.ToString() : "");
                if (readable)
                {
                    if (y < 10)
                    {
                        gardenString += " ";
                    }
                    gardenString += "| ";
                }
            }
            return gardenString;
        }
        /// <summary>
        /// ASCII method to alloww for printing of a readable graph
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(false);
        }
        private static double getDistance(Point start, Point end)
        {
            return Math.Sqrt(Math.Pow(start.X - end.X, 2) + Math.Pow(start.Y - end.Y, 2));
        }
        private static double getDistance(Atom start, Atom end)
        {
            return getDistance(start.getLocation(), end.getLocation());
        }
    }

}
